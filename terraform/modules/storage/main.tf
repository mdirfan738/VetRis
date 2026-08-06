# ---------------------------------------------------------------------------
# Storage module: DICOM active + archive S3 buckets.
#
# HIPAA notes:
#  - SSE-KMS encryption at rest on both buckets (CMK from security module)
#  - Versioning enabled (protects against accidental overwrite/delete)
#  - Public access fully blocked
#  - Access logging to a dedicated logs bucket
#  - TLS-only bucket policy (deny non-HTTPS requests)
#  - Lifecycle: active -> transitions to Glacier after 90 days;
#    archive bucket also transitions immediately for long-term retention.
# ---------------------------------------------------------------------------

locals {
  common_tags = merge(var.tags, {
    Project     = var.project
    Environment = var.environment
    ManagedBy   = "terraform"
  })
}

# ---- Access logging bucket ----
resource "aws_s3_bucket" "logs" {
  bucket = "${lower(var.project)}-s3-access-logs-${var.environment}"
  tags   = merge(local.common_tags, { Name = "${var.project}-s3-access-logs-${var.environment}" })
}

resource "aws_s3_bucket_public_access_block" "logs" {
  bucket                  = aws_s3_bucket.logs.id
  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_ownership_controls" "logs" {
  bucket = aws_s3_bucket.logs.id
  rule {
    object_ownership = "BucketOwnerEnforced"
  }
}

resource "aws_s3_bucket_lifecycle_configuration" "logs" {
  bucket = aws_s3_bucket.logs.id
  rule {
    id     = "expire-old-logs"
    status = "Enabled"
    filter {}
    expiration {
      days = 365
    }
  }
}

# ---- Active DICOM bucket ----
resource "aws_s3_bucket" "dicom_active" {
  bucket = "${lower(var.project)}-dicom-active-${var.environment}"
  tags   = merge(local.common_tags, { Name = "${var.project}-dicom-active-${var.environment}" })
}

resource "aws_s3_bucket_public_access_block" "dicom_active" {
  bucket                  = aws_s3_bucket.dicom_active.id
  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_versioning" "dicom_active" {
  bucket = aws_s3_bucket.dicom_active.id
  versioning_configuration {
    status = "Enabled"
  }
}

resource "aws_s3_bucket_server_side_encryption_configuration" "dicom_active" {
  bucket = aws_s3_bucket.dicom_active.id
  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm     = "aws:kms"
      kms_master_key_id = var.kms_key_arn
    }
    bucket_key_enabled = true
  }
}

resource "aws_s3_bucket_logging" "dicom_active" {
  bucket        = aws_s3_bucket.dicom_active.id
  target_bucket = aws_s3_bucket.logs.id
  target_prefix = "dicom-active/"
}

resource "aws_s3_bucket_lifecycle_configuration" "dicom_active" {
  bucket = aws_s3_bucket.dicom_active.id
  rule {
    id     = "archive-after-90-days"
    status = "Enabled"
    filter {}

    transition {
      days          = 90
      storage_class = "GLACIER"
    }

    noncurrent_version_transition {
      noncurrent_days = 30
      storage_class   = "GLACIER"
    }

    noncurrent_version_expiration {
      noncurrent_days = 365
    }
  }
}

resource "aws_s3_bucket_policy" "dicom_active_tls_only" {
  bucket = aws_s3_bucket.dicom_active.id
  policy = data.aws_iam_policy_document.tls_only[0].json
}

# ---- Archive DICOM bucket (long-term retention) ----
resource "aws_s3_bucket" "dicom_archive" {
  bucket = "${lower(var.project)}-dicom-archive-${var.environment}"
  tags   = merge(local.common_tags, { Name = "${var.project}-dicom-archive-${var.environment}" })
}

resource "aws_s3_bucket_public_access_block" "dicom_archive" {
  bucket                  = aws_s3_bucket.dicom_archive.id
  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_versioning" "dicom_archive" {
  bucket = aws_s3_bucket.dicom_archive.id
  versioning_configuration {
    status = "Enabled"
  }
}

resource "aws_s3_bucket_server_side_encryption_configuration" "dicom_archive" {
  bucket = aws_s3_bucket.dicom_archive.id
  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm     = "aws:kms"
      kms_master_key_id = var.kms_key_arn
    }
    bucket_key_enabled = true
  }
}

resource "aws_s3_bucket_logging" "dicom_archive" {
  bucket        = aws_s3_bucket.dicom_archive.id
  target_bucket = aws_s3_bucket.logs.id
  target_prefix = "dicom-archive/"
}

resource "aws_s3_bucket_lifecycle_configuration" "dicom_archive" {
  bucket = aws_s3_bucket.dicom_archive.id
  rule {
    id     = "immediate-glacier"
    status = "Enabled"
    filter {}
    transition {
      days          = 0
      storage_class = "GLACIER"
    }
  }
}

resource "aws_s3_bucket_policy" "dicom_archive_tls_only" {
  bucket = aws_s3_bucket.dicom_archive.id
  policy = data.aws_iam_policy_document.tls_only[1].json
}

data "aws_iam_policy_document" "tls_only" {
  count = 2
  statement {
    sid       = "DenyInsecureTransport"
    effect    = "Deny"
    actions   = ["s3:*"]
    resources = [
      count.index == 0 ? aws_s3_bucket.dicom_active.arn : aws_s3_bucket.dicom_archive.arn,
      count.index == 0 ? "${aws_s3_bucket.dicom_active.arn}/*" : "${aws_s3_bucket.dicom_archive.arn}/*",
    ]
    principals {
      type        = "AWS"
      identifiers = ["*"]
    }
    condition {
      test     = "Bool"
      variable = "aws:SecureTransport"
      values   = ["false"]
    }
  }
}
