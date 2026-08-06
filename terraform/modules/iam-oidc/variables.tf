variable "project" {
  type    = string
  default = "VetRIS"
}

variable "environment" {
  type = string
}

variable "github_org" {
  type        = string
  description = "GitHub org/user that owns the repo, e.g. my-org"
}

variable "github_repo" {
  type        = string
  description = "GitHub repo name, e.g. vetris-scaffold"
}

variable "allowed_branches" {
  type        = list(string)
  description = "Branches allowed to assume the CI/CD role (in addition to tags)"
  default     = ["main"]
}

variable "ecr_repository_arn" {
  type = string
}

variable "frontend_bucket_arn" {
  type = string
}

variable "cloudfront_distribution_arn" {
  type = string
}

variable "app_runner_service_arn" {
  type = string
}

variable "state_bucket_arn" {
  type        = string
  description = "ARN of the S3 bucket holding Terraform state (for the infra pipeline role)"
}

#variable "state_lock_table_arn" {
#  type        = string
#  description = "ARN of the DynamoDB table used for Terraform state locking"
#}

variable "kms_key_arn" {
  type = string
}

variable "tags" {
  type    = map(string)
  default = {}
}
