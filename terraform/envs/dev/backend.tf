# ---------------------------------------------------------------------------
# Terraform remote state backend.
#
# BOOTSTRAP NOTE: this S3 bucket + DynamoDB lock table must exist BEFORE
# `terraform init` can use this backend. Create them once, manually or via
# a small separate bootstrap script (see README.md "Step 1: Bootstrap").
# Fill in the bucket name after bootstrap (cannot use variables here).
# ---------------------------------------------------------------------------

terraform {
  backend "s3" {
    bucket         = "vishal-terraform-statefile"
    key            = "vetris/dev/terraform.tfstate"
    region         = "ap-south-1"
    #dynamodb_table = "{{YOUR_TF_LOCK_TABLE}}"
    encrypt        = true
  }
}
