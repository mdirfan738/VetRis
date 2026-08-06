# ---------------------------------------------------------------------------
# One-time bootstrap: creates the S3 bucket + DynamoDB table used as the
# Terraform remote state backend for the real environments (terraform/envs/*).
#
# This MUST be applied with local state (no backend block) since the
# backend it creates doesn't exist yet. Run this once per AWS account,
# then fill the resulting bucket/table names into:
#   terraform/envs/dev/backend.tf
#   terraform/envs/dev/terraform.tfvars
# ---------------------------------------------------------------------------

terraform {
  required_version = ">= 1.7.0"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = ">= 5.0"
    }
  }
}

provider "aws" {
  region = var.aws_region
}

variable "aws_region" {
  type    = string
  default = "us-east-1"
}

variable "state_bucket_name" {
  type        = string
  description = "Globally-unique name, e.g. vetris-tfstate-{{YOUR_ACCOUNT_ID}}"
}

variable "lock_table_name" {
  type    = string
  default = "vetris-tf-locks"
}

resource "aws_s3_bucket" "tf_state" {
  bucket = var.state_bucket_name
  tags   = { Project = "VetRIS", Purpose = "terraform-state" }
}

resource "aws_s3_bucket_versioning" "tf_state" {
  bucket = aws_s3_bucket.tf_state.id
  versioning_configuration {
    status = "Enabled"
  }
}

resource "aws_s3_bucket_server_side_encryption_configuration" "tf_state" {
  bucket = aws_s3_bucket.tf_state.id
  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "aws:kms"
    }
  }
}

resource "aws_s3_bucket_public_access_block" "tf_state" {
  bucket                  = aws_s3_bucket.tf_state.id
  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_dynamodb_table" "tf_locks" {
  name         = var.lock_table_name
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "LockID"

  attribute {
    name = "LockID"
    type = "S"
  }

  tags = { Project = "VetRIS", Purpose = "terraform-state-lock" }
}

output "state_bucket_name" {
  value = aws_s3_bucket.tf_state.bucket
}

output "state_bucket_arn" {
  value = aws_s3_bucket.tf_state.arn
}

output "lock_table_name" {
  value = aws_dynamodb_table.tf_locks.name
}

output "lock_table_arn" {
  value = aws_dynamodb_table.tf_locks.arn
}
