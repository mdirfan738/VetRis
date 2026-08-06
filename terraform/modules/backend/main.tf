# ---------------------------------------------------------------------------
# Backend module: ECR repo + App Runner service (private via VPC Connector
# to reach RDS in private subnets). An ECS Fargate + ALB alternative is
# included below, commented out, for teams that need more networking
# control (e.g. WAF directly in front of the API, or Fargate Spot).
# ---------------------------------------------------------------------------

locals {
  common_tags = merge(var.tags, {
    Project     = var.project
    Environment = var.environment
    ManagedBy   = "terraform"
  })

  db_connection_string = "server=${var.db_endpoint};database=${var.db_name};user=${var.db_master_username};password={{resolve:secretsmanager:${var.db_master_secret_arn}}}"
}

# ---- ECR ----
resource "aws_ecr_repository" "backend" {
  name                 = "${lower(var.project)}-${var.environment}-backend"
  image_tag_mutability = "IMMUTABLE"

  image_scanning_configuration {
    scan_on_push = true
  }

  encryption_configuration {
    encryption_type = "KMS"
    kms_key         = var.kms_key_arn
  }

  tags = local.common_tags
}

resource "aws_ecr_lifecycle_policy" "backend" {
  repository = aws_ecr_repository.backend.name
  policy = jsonencode({
    rules = [{
      rulePriority = 1
      description  = "Keep last 20 images"
      selection = {
        tagStatus   = "any"
        countType   = "imageCountMoreThan"
        countNumber = 20
      }
      action = { type = "expire" }
    }]
  })
}

# ---- App Runner VPC Connector (lets App Runner reach RDS in private subnets) ----
resource "aws_apprunner_vpc_connector" "this" {
  vpc_connector_name = "${lower(var.project)}-${var.environment}-connector"
  subnets             = var.private_subnet_ids
  security_groups      = [var.backend_security_group_id]

  tags = local.common_tags
}

# ---- IAM role App Runner assumes to build/pull from ECR ----
data "aws_iam_policy_document" "apprunner_access_assume" {
  statement {
    effect  = "Allow"
    actions = ["sts:AssumeRole"]
    principals {
      type        = "Service"
      identifiers = ["build.apprunner.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "apprunner_access" {
  name               = "${lower(var.project)}-${var.environment}-apprunner-access"
  assume_role_policy = data.aws_iam_policy_document.apprunner_access_assume.json
  tags               = local.common_tags
}

resource "aws_iam_role_policy_attachment" "apprunner_ecr_access" {
  role       = aws_iam_role.apprunner_access.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSAppRunnerServicePolicyForECRAccess"
}

# ---- IAM role the *running task* uses (least privilege: S3 upload + Secrets read) ----
data "aws_iam_policy_document" "apprunner_task_assume" {
  statement {
    effect  = "Allow"
    actions = ["sts:AssumeRole"]
    principals {
      type        = "Service"
      identifiers = ["tasks.apprunner.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "apprunner_task" {
  name               = "${lower(var.project)}-${var.environment}-apprunner-task"
  assume_role_policy = data.aws_iam_policy_document.apprunner_task_assume.json
  tags               = local.common_tags
}

data "aws_iam_policy_document" "apprunner_task_permissions" {
  statement {
    sid    = "S3UploadActiveBucket"
    effect = "Allow"
    actions = [
      "s3:PutObject",
      "s3:GetObject",
      "s3:ListBucket"
    ]
    resources = [
      var.dicom_active_bucket_arn,
      "${var.dicom_active_bucket_arn}/*"
    ]
  }

  statement {
    sid       = "KmsForS3"
    effect    = "Allow"
    actions   = ["kms:Decrypt", "kms:GenerateDataKey"]
    resources = [var.kms_key_arn]
  }

  statement {
    sid       = "ReadDbSecret"
    effect    = "Allow"
    actions   = ["secretsmanager:GetSecretValue"]
    resources = [var.db_master_secret_arn]
  }

  statement {
    sid    = "Logs"
    effect = "Allow"
    actions = [
      "logs:CreateLogStream",
      "logs:PutLogEvents"
    ]
    resources = ["*"]
  }
}

resource "aws_iam_role_policy" "apprunner_task" {
  name   = "${lower(var.project)}-${var.environment}-apprunner-task-policy"
  role   = aws_iam_role.apprunner_task.id
  policy = data.aws_iam_policy_document.apprunner_task_permissions.json
}

# ---- App Runner service ----
resource "aws_apprunner_service" "backend" {
  service_name = "${lower(var.project)}-${var.environment}-backend"

  source_configuration {
    authentication_configuration {
      access_role_arn = aws_iam_role.apprunner_access.arn
    }

    auto_deployments_enabled = false # CI/CD triggers deploys explicitly (see backend-deploy.yml)

    image_repository {
      image_identifier      = "${aws_ecr_repository.backend.repository_url}:latest"
      image_repository_type = "ECR"

      image_configuration {
        port = "8080"
        runtime_environment_variables = {
          ALLOWED_ORIGIN      = var.allowed_origin
          Aws__DicomActiveBucket = var.dicom_active_bucket_name
        }
        runtime_environment_secrets = {
          DB_CONNECTION_STRING = "${var.db_master_secret_arn}"
        }
      }
    }
  }

  instance_configuration {
    cpu               = var.cpu
    memory             = var.memory
    instance_role_arn  = aws_iam_role.apprunner_task.arn
  }

  network_configuration {
    egress_configuration {
      egress_type       = "VPC"
      vpc_connector_arn = aws_apprunner_vpc_connector.this.arn
    }
  }

  auto_scaling_configuration_arn = aws_apprunner_auto_scaling_configuration_version.this.arn

  health_check_configuration {
    protocol = "HTTP"
    path     = "/api/health"
    interval = 10
    timeout  = 5
  }

  observability_configuration {
    observability_enabled = false
  }

  tags = local.common_tags
}

resource "aws_apprunner_auto_scaling_configuration_version" "this" {
  auto_scaling_configuration_name = "${lower(var.project)}-${var.environment}-scaling"
  min_size                        = var.min_size
  max_size                        = var.max_size
  max_concurrency                 = 100

  tags = local.common_tags
}

# ---------------------------------------------------------------------------
# ALTERNATIVE: ECS Fargate + ALB (commented out)
#
# Use this instead of App Runner if you need: WAF directly on the API (not
# just CloudFront/frontend), private ALB + VPN/PrivateLink access, Fargate
# Spot for cost savings, or finer-grained task networking control.
#
# resource "aws_ecs_cluster" "this" {
#   name = "${lower(var.project)}-${var.environment}-cluster"
#   setting {
#     name  = "containerInsights"
#     value = "enabled"
#   }
#   tags = local.common_tags
# }
#
# resource "aws_ecs_task_definition" "backend" {
#   family                   = "${lower(var.project)}-${var.environment}-backend"
#   requires_compatibilities = ["FARGATE"]
#   network_mode             = "awsvpc"
#   cpu                      = "1024"
#   memory                   = "2048"
#   execution_role_arn       = aws_iam_role.apprunner_access.arn # reuse or create ecsTaskExecutionRole
#   task_role_arn             = aws_iam_role.apprunner_task.arn
#   container_definitions = jsonencode([{
#     name      = "backend"
#     image     = "${aws_ecr_repository.backend.repository_url}:latest"
#     essential = true
#     portMappings = [{ containerPort = 8080, protocol = "tcp" }]
#     logConfiguration = {
#       logDriver = "awslogs"
#       options = {
#         "awslogs-group"         = var.log_group_name
#         "awslogs-region"        = "us-east-1"
#         "awslogs-stream-prefix" = "backend"
#       }
#     }
#   }])
#   tags = local.common_tags
# }
#
# resource "aws_ecs_service" "backend" {
#   name            = "${lower(var.project)}-${var.environment}-backend"
#   cluster         = aws_ecs_cluster.this.id
#   task_definition = aws_ecs_task_definition.backend.arn
#   desired_count   = var.min_size
#   launch_type     = "FARGATE"
#
#   network_configuration {
#     subnets          = var.private_subnet_ids
#     security_groups  = [var.backend_security_group_id]
#     assign_public_ip = false
#   }
#   # Attach to an internal or internet-facing ALB target group as needed.
#   tags = local.common_tags
# }
# ---------------------------------------------------------------------------
