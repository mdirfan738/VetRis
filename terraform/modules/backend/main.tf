# ---------------------------------------------------------------------------
# VetRIS backend compute — ECS Fargate + Application Load Balancer.
#
# VetRIS backend compute: ECS Fargate tasks behind an Application Load
# Balancer, with TLS via ACM and a Route53 alias record for
# api.<domain_name>.
# ---------------------------------------------------------------------------

data "aws_region" "current" {}

# ---------------------------------------------------------------------------
# ECR repository
# ---------------------------------------------------------------------------
resource "aws_ecr_repository" "backend" {
  name                 = "${lower(var.project)}-${var.environment}-backend"
  image_tag_mutability = "MUTABLE"
  # Lab-mode: lets `terraform destroy` remove this repo even with images
  # still in it, so cleanup doesn't need a separate "empty the repo" step.
  # Turn off for a real production repo you don't want destroy-able by accident.
  force_delete = true

  image_scanning_configuration {
    scan_on_push = true
  }

  encryption_configuration {
    encryption_type = "KMS"
    kms_key         = var.kms_key_arn
  }

  tags = var.tags
}

resource "aws_ecr_lifecycle_policy" "backend" {
  repository = aws_ecr_repository.backend.name
  policy = jsonencode({
    rules = [{
      rulePriority = 1
      description  = "Keep last 10 images"
      selection = {
        tagStatus   = "any"
        countType   = "imageCountMoreThan"
        countNumber = 10
      }
      action = { type = "expire" }
    }]
  })
}

# ---------------------------------------------------------------------------
# Fixed admin credentials (DB password + app login) — explicit requirement,
# not the auto-generated-random-secret pattern used elsewhere in this repo.
# Stored in Secrets Manager (not a literal in any .tf/.tfvars file) so
# `terraform plan/apply` output and state-inspection tools don't print them
# in plaintext by accident, but the VALUE itself is the fixed one requested,
# not randomly generated.
# ---------------------------------------------------------------------------
resource "aws_secretsmanager_secret" "admin_credentials" {
  name       = "${lower(var.project)}-${var.environment}-admin-credentials"
  kms_key_id = var.kms_key_arn
  tags       = var.tags
}

resource "aws_secretsmanager_secret_version" "admin_credentials" {
  secret_id = aws_secretsmanager_secret.admin_credentials.id
  secret_string = jsonencode({
    username = var.admin_username
    password = var.admin_password
  })
}

# Internal JWT signing key — NOT a login credential a person types in, so
# the fixed-credentials requirement doesn't apply here; this stays randomly
# generated and strong.
resource "random_password" "jwt_key" {
  length  = 64
  special = true
}

resource "aws_secretsmanager_secret" "jwt_key" {
  name       = "${lower(var.project)}-${var.environment}-jwt-key"
  kms_key_id = var.kms_key_arn
  tags       = var.tags
}

resource "aws_secretsmanager_secret_version" "jwt_key" {
  secret_id     = aws_secretsmanager_secret.jwt_key.id
  secret_string = random_password.jwt_key.result
}

# ---------------------------------------------------------------------------
# ECS cluster
# ---------------------------------------------------------------------------
resource "aws_ecs_cluster" "this" {
  name = "${lower(var.project)}-${var.environment}-cluster"

  setting {
    name  = "containerInsights"
    value = "enabled"
  }

  tags = var.tags
}

# ---------------------------------------------------------------------------
# IAM — task execution role (pull image, write logs, read secrets) and task
# role (what the running app itself can call — S3, mirrors the old App
# Runner instance role's intent).
# ---------------------------------------------------------------------------
data "aws_iam_policy_document" "ecs_tasks_trust" {
  statement {
    effect  = "Allow"
    actions = ["sts:AssumeRole"]
    principals {
      type        = "Service"
      identifiers = ["ecs-tasks.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "ecs_execution" {
  name               = "${lower(var.project)}-${var.environment}-ecs-execution"
  assume_role_policy = data.aws_iam_policy_document.ecs_tasks_trust.json
  tags               = var.tags
}

resource "aws_iam_role_policy_attachment" "ecs_execution_managed" {
  role       = aws_iam_role.ecs_execution.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"
}

data "aws_iam_policy_document" "ecs_execution_secrets" {
  statement {
    effect = "Allow"
    actions = ["secretsmanager:GetSecretValue"]
    resources = [
      var.db_master_secret_arn,
      aws_secretsmanager_secret.jwt_key.arn,
      aws_secretsmanager_secret.admin_credentials.arn,
    ]
  }
  statement {
    effect    = "Allow"
    actions   = ["kms:Decrypt"]
    resources = [var.kms_key_arn]
  }
}

resource "aws_iam_role_policy" "ecs_execution_secrets" {
  name   = "${lower(var.project)}-${var.environment}-ecs-execution-secrets"
  role   = aws_iam_role.ecs_execution.id
  policy = data.aws_iam_policy_document.ecs_execution_secrets.json
}

resource "aws_iam_role" "ecs_task" {
  name               = "${lower(var.project)}-${var.environment}-ecs-task"
  assume_role_policy = data.aws_iam_policy_document.ecs_tasks_trust.json
  tags               = var.tags
}

data "aws_iam_policy_document" "ecs_task_permissions" {
  statement {
    sid    = "DicomBucketAccess"
    effect = "Allow"
    actions = [
      "s3:GetObject", "s3:PutObject", "s3:CopyObject", "s3:DeleteObject", "s3:ListBucket"
    ]
    resources = [var.dicom_active_bucket_arn, "${var.dicom_active_bucket_arn}/*"]
  }
}

resource "aws_iam_role_policy" "ecs_task" {
  name   = "${lower(var.project)}-${var.environment}-ecs-task-policy"
  role   = aws_iam_role.ecs_task.id
  policy = data.aws_iam_policy_document.ecs_task_permissions.json
}

# ---------------------------------------------------------------------------
# ALB — internet-facing, public subnets, TLS via ACM
# ---------------------------------------------------------------------------
resource "aws_security_group" "alb" {
  name_prefix = "${var.project}-${var.environment}-backend-alb-"
  description = "SG for the VetRIS backend ALB"
  vpc_id      = var.vpc_id

  ingress {
    description = "HTTPS from the internet"
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  ingress {
    description = "HTTP from the internet (redirected to HTTPS below)"
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = merge(var.tags, { Name = "${var.project}-${var.environment}-backend-alb-sg" })

  lifecycle {
    create_before_destroy = true
  }
}

# The existing backend security group (module.network) has no ingress rule
# yet — this is the one it actually needs for the ALB to reach the tasks,
# added here rather than by editing the network module.
resource "aws_security_group_rule" "backend_from_alb" {
  type                     = "ingress"
  from_port                = var.container_port
  to_port                  = var.container_port
  protocol                 = "tcp"
  security_group_id        = var.backend_security_group_id
  source_security_group_id = aws_security_group.alb.id
  description              = "Allow the backend ALB to reach the ECS tasks"
}

resource "aws_lb" "backend" {
  name               = "${lower(var.project)}-${var.environment}-backend"
  internal           = false
  load_balancer_type = "application"
  security_groups    = [aws_security_group.alb.id]
  subnets            = var.public_subnet_ids

  tags = var.tags
}

resource "aws_lb_target_group" "backend" {
  name        = "${lower(var.project)}-${var.environment}-backend-tg"
  port        = var.container_port
  protocol    = "HTTP"
  vpc_id      = var.vpc_id
  target_type = "ip"

  health_check {
    path                = "/api/health"
    healthy_threshold   = 2
    unhealthy_threshold = 3
    interval            = 15
    timeout             = 5
    matcher             = "200"
  }

  tags = var.tags
}

resource "aws_acm_certificate" "backend" {
  domain_name       = var.api_domain_name
  validation_method = "DNS"

  lifecycle {
    create_before_destroy = true
  }

  tags = var.tags
}

resource "aws_route53_record" "backend_cert_validation" {
  for_each = {
    for dvo in aws_acm_certificate.backend.domain_validation_options : dvo.domain_name => {
      name   = dvo.resource_record_name
      record = dvo.resource_record_value
      type   = dvo.resource_record_type
    }
  }

  zone_id         = var.route53_zone_id
  name            = each.value.name
  type            = each.value.type
  records         = [each.value.record]
  ttl             = 60
  allow_overwrite = true
}

resource "aws_acm_certificate_validation" "backend" {
  certificate_arn         = aws_acm_certificate.backend.arn
  validation_record_fqdns = [for r in aws_route53_record.backend_cert_validation : r.fqdn]
}

resource "aws_lb_listener" "https" {
  load_balancer_arn = aws_lb.backend.arn
  port              = 443
  protocol          = "HTTPS"
  ssl_policy        = "ELBSecurityPolicy-TLS13-1-2-2021-06"
  certificate_arn   = aws_acm_certificate_validation.backend.certificate_arn

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.backend.arn
  }
}

resource "aws_lb_listener" "http_redirect" {
  load_balancer_arn = aws_lb.backend.arn
  port              = 80
  protocol          = "HTTP"

  default_action {
    type = "redirect"
    redirect {
      port        = "443"
      protocol    = "HTTPS"
      status_code = "HTTP_301"
    }
  }
}

resource "aws_route53_record" "api" {
  zone_id = var.route53_zone_id
  name    = var.api_domain_name
  type    = "A"

  alias {
    name                   = aws_lb.backend.dns_name
    zone_id                = aws_lb.backend.zone_id
    evaluate_target_health = true
  }
}

# ---------------------------------------------------------------------------
# ECS task definition + service
# ---------------------------------------------------------------------------
resource "aws_cloudwatch_log_group" "backend_override" {
  count             = var.log_group_name == "" ? 1 : 0
  name              = "/ecs/${lower(var.project)}-${var.environment}-backend"
  retention_in_days = 30
  kms_key_id        = var.kms_key_arn
  tags              = var.tags
}

locals {
  log_group_name = var.log_group_name != "" ? var.log_group_name : aws_cloudwatch_log_group.backend_override[0].name
}

resource "aws_ecs_task_definition" "backend" {
  family                   = "${lower(var.project)}-${var.environment}-backend"
  requires_compatibilities = ["FARGATE"]
  network_mode             = "awsvpc"
  cpu                      = var.task_cpu
  memory                   = var.task_memory
  execution_role_arn       = aws_iam_role.ecs_execution.arn
  task_role_arn            = aws_iam_role.ecs_task.arn

  container_definitions = jsonencode([
    {
      name      = "backend"
      image     = "${aws_ecr_repository.backend.repository_url}:latest"
      essential = true
      portMappings = [{
        containerPort = var.container_port
        protocol      = "tcp"
      }]
      environment = [
        { name = "ASPNETCORE_ENVIRONMENT", value = "Production" },
        { name = "ALLOWED_ORIGIN", value = var.allowed_origin },
        { name = "Aws__DicomActiveBucket", value = var.dicom_active_bucket_name },
        { name = "Jwt__Issuer", value = "https://${var.api_domain_name}" },
        { name = "Jwt__Audience", value = "VetRIS.Clients" },
        { name = "Pacs__Host", value = var.pacs_host },
      ]
      secrets = [
        { name = "DB_CONNECTION_STRING", valueFrom = var.db_master_secret_arn },
        { name = "Jwt__Key", valueFrom = aws_secretsmanager_secret.jwt_key.arn },
        { name = "VETRIS_ADMIN_USERNAME", valueFrom = "${aws_secretsmanager_secret.admin_credentials.arn}:username::" },
        { name = "VETRIS_ADMIN_PASSWORD", valueFrom = "${aws_secretsmanager_secret.admin_credentials.arn}:password::" },
      ]
      logConfiguration = {
        logDriver = "awslogs"
        options = {
          "awslogs-group"         = local.log_group_name
          "awslogs-region"        = data.aws_region.current.name
          "awslogs-stream-prefix" = "backend"
        }
      }
    }
  ])

  tags = var.tags
}

resource "aws_ecs_service" "backend" {
  name            = "${lower(var.project)}-${var.environment}-backend"
  cluster         = aws_ecs_cluster.this.id
  task_definition = aws_ecs_task_definition.backend.arn
  desired_count   = var.desired_count
  launch_type     = "FARGATE"

  network_configuration {
    subnets          = var.private_subnet_ids
    security_groups  = [var.backend_security_group_id]
    assign_public_ip = false
  }

  load_balancer {
    target_group_arn = aws_lb_target_group.backend.arn
    container_name   = "backend"
    container_port   = var.container_port
  }

  deployment_minimum_healthy_percent = 100
  deployment_maximum_percent         = 200

  depends_on = [aws_lb_listener.https]

  lifecycle {
    # The deploy pipeline updates the running task definition via
    # `aws ecs update-service --force-new-deployment` after pushing a new
    # image — Terraform shouldn't fight that by reverting it back on the
    # next `apply`.
    ignore_changes = [task_definition]
  }

  tags = var.tags
}

# ---------------------------------------------------------------------------
# Auto scaling — target CPU utilization
# ---------------------------------------------------------------------------
resource "aws_appautoscaling_target" "backend" {
  max_capacity       = var.max_capacity
  min_capacity       = var.min_capacity
  resource_id        = "service/${aws_ecs_cluster.this.name}/${aws_ecs_service.backend.name}"
  scalable_dimension = "ecs:service:DesiredCount"
  service_namespace  = "ecs"
}

resource "aws_appautoscaling_policy" "backend_cpu" {
  name               = "${lower(var.project)}-${var.environment}-backend-cpu"
  policy_type        = "TargetTrackingScaling"
  resource_id        = aws_appautoscaling_target.backend.resource_id
  scalable_dimension = aws_appautoscaling_target.backend.scalable_dimension
  service_namespace  = aws_appautoscaling_target.backend.service_namespace

  target_tracking_scaling_policy_configuration {
    predefined_metric_specification {
      predefined_metric_type = "ECSServiceAverageCPUUtilization"
    }
    target_value       = 65
    scale_in_cooldown  = 120
    scale_out_cooldown = 60
  }
}
