variable "project" {
  type = string
}

variable "environment" {
  type = string
}

variable "vpc_id" {
  type        = string
  description = "From module.network.vpc_id"
}

variable "public_subnet_ids" {
  type        = list(string)
  description = "For the ALB — from module.network.public_subnet_ids"
}

variable "private_subnet_ids" {
  type        = list(string)
  description = "For the ECS tasks — from module.network.private_subnet_ids"
}

variable "backend_security_group_id" {
  type        = string
  description = "From module.network.backend_security_group_id — this module adds the ALB-ingress rule to it"
}

variable "kms_key_arn" {
  type = string
}

variable "db_endpoint" {
  type = string
}

variable "db_name" {
  type = string
}

variable "db_master_username" {
  type = string
}

variable "db_master_secret_arn" {
  type        = string
  description = "Secrets Manager ARN holding the fixed DB password (see modules/security)"
}

variable "dicom_active_bucket_name" {
  type = string
}

variable "dicom_active_bucket_arn" {
  type = string
}

variable "allowed_origin" {
  type = string
}

variable "log_group_name" {
  type = string
}

variable "api_domain_name" {
  type        = string
  description = "Full backend API domain, e.g. api.{{YOUR_DOMAIN}} — same 'already-full-hostname' convention as the frontend module's domain_name, not a root domain this module derives a subdomain from"
}

variable "route53_zone_id" {
  type = string
}

variable "container_port" {
  type    = number
  default = 8080
}

variable "task_cpu" {
  type        = number
  default     = 512
  description = "vCPU units (256 = .25 vCPU); 512 = .5 vCPU is a reasonable dev default"
}

variable "task_memory" {
  type    = number
  default = 1024
}

variable "desired_count" {
  type    = number
  default = 1
}

variable "min_capacity" {
  type    = number
  default = 1
}

variable "max_capacity" {
  type    = number
  default = 3
}

variable "pacs_host" {
  type    = string
  default = ""
}

variable "admin_username" {
  type        = string
  default     = "Admin"
  description = "Fixed per explicit requirement — see README.md's Security Practices section for the lab-vs-production tradeoff this implies"
}

variable "admin_password" {
  type        = string
  default     = "Admin"
  sensitive   = true
  description = "Fixed per explicit requirement. The app's Identity password policy is relaxed specifically to accept this — see backend/VetRIS.Api/Program.cs"
}

variable "tags" {
  type    = map(string)
  default = {}
}
