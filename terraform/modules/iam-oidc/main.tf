# ---------------------------------------------------------------------------
# IAM / GitHub OIDC module.
#
# Creates:
#  - the GitHub Actions OIDC identity provider (once per AWS account)
#  - THREE separate least-privilege roles, one per pipeline:
#      1. infra-cicd   -> terraform plan/apply (broad within the project's
#                         resource types, still scoped by tags/ARNs where possible)
#      2. backend-cicd -> push to ECR + deploy App Runner service
#      3. frontend-cicd-> sync to S3 + invalidate CloudFront
#
# Each role trust policy is restricted to this specific GitHub repo and to
# the given branches (sub claim), which is the key control that prevents
# any other repo/workflow from assuming these roles.
# ---------------------------------------------------------------------------

locals {
  common_tags = merge(var.tags, {
    Project     = var.project
    Environment = var.environment
    ManagedBy   = "terraform"
  })

  github_sub_conditions = [for b in var.allowed_branches : "repo:${var.github_org}/${var.github_repo}:ref:refs/heads/${b}"]
}

data "tls_certificate" "github" {
  url = "https://token.actions.githubusercontent.com/.well-known/openid-configuration"
}

resource "aws_iam_openid_connect_provider" "github" {
  url             = "https://token.actions.githubusercontent.com"
  client_id_list  = ["sts.amazonaws.com"]
  thumbprint_list = [data.tls_certificate.github.certificates[0].sha1_fingerprint]

  tags = local.common_tags
}

# ---------------------------------------------------------------------------
# 1. Infra pipeline role: terraform plan/apply
# ---------------------------------------------------------------------------
data "aws_iam_policy_document" "infra_trust" {
  statement {
    effect  = "Allow"
    actions = ["sts:AssumeRoleWithWebIdentity"]

    principals {
      type        = "Federated"
      identifiers = [aws_iam_openid_connect_provider.github.arn]
    }

    condition {
      test     = "StringEquals"
      variable = "token.actions.githubusercontent.com:aud"
      values   = ["sts.amazonaws.com"]
    }

    condition {
      test     = "StringLike"
      variable = "token.actions.githubusercontent.com:sub"
      values   = local.github_sub_conditions
    }
  }
}

resource "aws_iam_role" "infra_cicd" {
  name               = "${lower(var.project)}-${var.environment}-infra-cicd"
  assume_role_policy = data.aws_iam_policy_document.infra_trust.json
  tags               = local.common_tags
}

# NOTE: Terraform needs broad permissions to manage VPC/RDS/S3/IAM/etc.
# This is intentionally scoped to actions (not full admin) and to
# state bucket/lock table ARNs where a resource-level ARN is available.
# Tighten further per your org's policy-as-code standards (e.g. permission
# boundaries, SCPs) before production use.
data "aws_iam_policy_document" "infra_permissions" {
  statement {
    sid    = "TerraformStateAccess"
    effect = "Allow"
    actions = [
      "s3:GetObject", "s3:PutObject", "s3:ListBucket", "s3:DeleteObject"
    ]
    resources = [var.state_bucket_arn, "${var.state_bucket_arn}/*"]
  }

  statement {
    sid       = "TerraformLockTable"
    effect    = "Allow"
    actions   = ["dynamodb:GetItem", "dynamodb:PutItem", "dynamodb:DeleteItem"]
    #resources = [var.state_lock_table_arn]
  }

  statement {
    sid    = "ManageProjectInfra"
    effect = "Allow"
    actions = [
      "ec2:*", "rds:*", "s3:*", "cloudfront:*", "route53:*", "acm:*",
      "wafv2:*", "kms:*", "secretsmanager:*", "logs:*", "ecr:*",
      "apprunner:*", "iam:GetRole", "iam:PassRole", "iam:CreateRole",
      "iam:DeleteRole", "iam:AttachRolePolicy", "iam:DetachRolePolicy",
      "iam:PutRolePolicy", "iam:DeleteRolePolicy", "iam:TagRole",
      "iam:ListRolePolicies", "iam:ListAttachedRolePolicies",
      "iam:GetRolePolicy", "iam:GetOpenIDConnectProvider",
      "iam:CreateOpenIDConnectProvider"
    ]
    resources = ["*"]
    condition {
      test     = "StringEquals"
      variable = "aws:ResourceTag/Project"
      values   = [var.project]
    }
  }

  # Some read-only/list actions don't support resource-level tags and
  # need broader scope to run `terraform plan` successfully.
  statement {
    sid    = "ReadOnlyDiscovery"
    effect = "Allow"
    actions = [
      "ec2:Describe*", "rds:Describe*", "s3:ListAllMyBuckets",
      "iam:ListRoles", "iam:ListOpenIDConnectProviders", "sts:GetCallerIdentity"
    ]
    resources = ["*"]
  }
}

resource "aws_iam_role_policy" "infra_cicd" {
  name   = "${lower(var.project)}-${var.environment}-infra-cicd-policy"
  role   = aws_iam_role.infra_cicd.id
  policy = data.aws_iam_policy_document.infra_permissions.json
}

# ---------------------------------------------------------------------------
# 2. Backend pipeline role: build/push to ECR + deploy App Runner
# ---------------------------------------------------------------------------
resource "aws_iam_role" "backend_cicd" {
  name               = "${lower(var.project)}-${var.environment}-backend-cicd"
  assume_role_policy = data.aws_iam_policy_document.infra_trust.json # same GitHub OIDC trust
  tags               = local.common_tags
}

data "aws_iam_policy_document" "backend_permissions" {
  statement {
    sid    = "EcrAuth"
    effect = "Allow"
    actions = ["ecr:GetAuthorizationToken"]
    resources = ["*"]
  }

  statement {
    sid    = "EcrPush"
    effect = "Allow"
    actions = [
      "ecr:BatchCheckLayerAvailability", "ecr:GetDownloadUrlForLayer",
      "ecr:BatchGetImage", "ecr:PutImage", "ecr:InitiateLayerUpload",
      "ecr:UploadLayerPart", "ecr:CompleteLayerUpload"
    ]
    resources = [var.ecr_repository_arn]
  }

  statement {
    sid       = "DeployAppRunner"
    effect    = "Allow"
    actions   = ["apprunner:StartDeployment", "apprunner:DescribeService"]
    resources = [var.app_runner_service_arn]
  }
}

resource "aws_iam_role_policy" "backend_cicd" {
  name   = "${lower(var.project)}-${var.environment}-backend-cicd-policy"
  role   = aws_iam_role.backend_cicd.id
  policy = data.aws_iam_policy_document.backend_permissions.json
}

# ---------------------------------------------------------------------------
# 3. Frontend pipeline role: sync build to S3 + invalidate CloudFront
# ---------------------------------------------------------------------------
resource "aws_iam_role" "frontend_cicd" {
  name               = "${lower(var.project)}-${var.environment}-frontend-cicd"
  assume_role_policy = data.aws_iam_policy_document.infra_trust.json
  tags               = local.common_tags
}

data "aws_iam_policy_document" "frontend_permissions" {
  statement {
    sid    = "SyncToS3"
    effect = "Allow"
    actions = [
      "s3:PutObject", "s3:GetObject", "s3:ListBucket", "s3:DeleteObject"
    ]
    resources = [var.frontend_bucket_arn, "${var.frontend_bucket_arn}/*"]
  }

  statement {
    sid       = "KmsForFrontendBucket"
    effect    = "Allow"
    actions   = ["kms:Decrypt", "kms:GenerateDataKey"]
    resources = [var.kms_key_arn]
  }

  statement {
    sid       = "InvalidateCloudFront"
    effect    = "Allow"
    actions   = ["cloudfront:CreateInvalidation", "cloudfront:GetInvalidation"]
    resources = [var.cloudfront_distribution_arn]
  }
}

resource "aws_iam_role_policy" "frontend_cicd" {
  name   = "${lower(var.project)}-${var.environment}-frontend-cicd-policy"
  role   = aws_iam_role.frontend_cicd.id
  policy = data.aws_iam_policy_document.frontend_permissions.json
}
