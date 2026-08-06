variable "aws_region" {
  type    = string
  default = "us-east-1"
}

variable "environment" {
  type    = string
  default = "dev"
}

variable "azs" {
  type    = list(string)
  default = ["us-east-1a", "us-east-1b"]
}

variable "domain_name" {
  type        = string
  description = "Full frontend domain, e.g. app.{{YOUR_DOMAIN}}"
  default     = "app.{{YOUR_DOMAIN}}"
}

variable "route53_zone_id" {
  type        = string
  description = "Existing Route53 hosted zone id for {{YOUR_DOMAIN}}"
}

variable "backend_allowed_origin" {
  type        = string
  description = "Origin allowed by backend CORS policy, e.g. https://app.{{YOUR_DOMAIN}}"
  default     = "https://app.{{YOUR_DOMAIN}}"
}

variable "github_org" {
  type    = string
  default = "{{YOUR_GITHUB_ORG}}"
}

variable "github_repo" {
  type    = string
  default = "vetris-scaffold"
}

variable "tf_state_bucket_arn" {
  type        = string
  description = "ARN of the bucket created during bootstrap (see README Step 1)"
}

#variable "tf_lock_table_arn" {
#  type        = string
#  description = "ARN of the DynamoDB lock table created during bootstrap"
#}

variable "db_instance_class" {
  type    = string
  default = "db.t4g.small"
}

variable "multi_az" {
  type    = bool
  default = false
}
