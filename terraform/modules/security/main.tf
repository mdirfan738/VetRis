# ---------------------------------------------------------------------------
# Security module: KMS CMK (used by RDS, S3, Secrets Manager, CloudWatch Logs),
# CloudWatch Log Groups, WAFv2 WebACL for CloudFront, Secrets Manager secret
# for the DB password.
# ---------------------------------------------------------------------------

locals {
  common_tags = merge(var.tags, {
    Project     = var.project
    Environment = var.environment
    ManagedBy   = "terraform"
  })
}

data "aws_caller_identity" "current" {}

resource "aws_kms_key" "vetris" {
  description             = "VetRIS ${var.environment} CMK - RDS, S3, Secrets Manager, CloudWatch Logs"
  deletion_window_in_days = 30
  enable_key_rotation     = true

  tags = merge(local.common_tags, { Name = "${var.project}-${var.environment}-cmk" })
}

resource "aws_kms_alias" "vetris" {
  name          = "alias/${lower(var.project)}-${var.environment}"
  target_key_id = aws_kms_key.vetris.key_id
}

# ---- CloudWatch Log Groups ----
resource "aws_cloudwatch_log_group" "backend" {
  name              = "/vetris/${var.environment}/backend"
  retention_in_days = 90
  kms_key_id        = aws_kms_key.vetris.arn

  tags = local.common_tags
}

resource "aws_cloudwatch_log_group" "backend_ecs" {
  name              = "/vetris/${var.environment}/backend-ecs"
  retention_in_days = 90
  kms_key_id        = aws_kms_key.vetris.arn

  tags = local.common_tags
}

# ---- Secrets Manager: DB master password ----
# Fixed value per explicit requirement (not randomly generated like the
# JWT key below) — RDS MySQL requires 8+ characters, so "AdminAdmin" is
# the smallest change from the literal "Admin" that's actually accepted by
# the platform. See README.md's Security Practices section for the
# lab-vs-production tradeoff this implies.
locals {
  db_master_password_fixed = "AdminAdmin"
}

resource "aws_secretsmanager_secret" "db_master" {
  name        = "vetris/${var.environment}/db-master-password"
  description = "RDS MySQL master password for VetRIS ${var.environment}"
  kms_key_id  = aws_kms_key.vetris.arn

  tags = local.common_tags
}

resource "aws_secretsmanager_secret_version" "db_master" {
  secret_id     = aws_secretsmanager_secret.db_master.id
  secret_string = local.db_master_password_fixed
}

# ---- WAFv2 WebACL for CloudFront (must be created in us-east-1) ----
resource "aws_wafv2_web_acl" "cloudfront" {
  provider    = aws.us_east_1
  name        = "${var.project}-${var.environment}-cf-waf"
  description = "WAF for VetRIS CloudFront distribution"
  scope       = "CLOUDFRONT"

  default_action {
    allow {}
  }

  rule {
    name     = "AWS-AWSManagedRulesCommonRuleSet"
    priority = 1

    override_action {
      none {}
    }

    statement {
      managed_rule_group_statement {
        name        = "AWSManagedRulesCommonRuleSet"
        vendor_name = "AWS"
      }
    }

    visibility_config {
      cloudwatch_metrics_enabled = true
      metric_name                = "${var.project}-${var.environment}-common-rules"
      sampled_requests_enabled   = true
    }
  }

  rule {
    name     = "AWS-AWSManagedRulesKnownBadInputsRuleSet"
    priority = 2

    override_action {
      none {}
    }

    statement {
      managed_rule_group_statement {
        name        = "AWSManagedRulesKnownBadInputsRuleSet"
        vendor_name = "AWS"
      }
    }

    visibility_config {
      cloudwatch_metrics_enabled = true
      metric_name                = "${var.project}-${var.environment}-bad-inputs"
      sampled_requests_enabled   = true
    }
  }

  rule {
    name     = "RateLimit"
    priority = 3

    action {
      block {}
    }

    statement {
      rate_based_statement {
        limit              = 2000
        aggregate_key_type = "IP"
      }
    }

    visibility_config {
      cloudwatch_metrics_enabled = true
      metric_name                = "${var.project}-${var.environment}-rate-limit"
      sampled_requests_enabled   = true
    }
  }

  visibility_config {
    cloudwatch_metrics_enabled = true
    metric_name                = "${var.project}-${var.environment}-webacl"
    sampled_requests_enabled   = true
  }

  tags = local.common_tags
}
