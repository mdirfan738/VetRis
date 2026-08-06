variable "project" {
  type    = string
  default = "VetRIS"
}

variable "environment" {
  type = string
}

variable "vpc_id" {
  type = string
}

variable "private_subnet_ids" {
  type = list(string)
}

variable "database_security_group_id" {
  type = string
}

variable "kms_key_arn" {
  type = string
}

variable "db_master_username" {
  type    = string
  default = "vetris_admin"
}

variable "db_master_password" {
  type      = string
  sensitive = true
}

variable "instance_class" {
  type    = string
  default = "db.t4g.small"
}

variable "allocated_storage_gb" {
  type    = number
  default = 50
}

variable "backup_retention_days" {
  type    = number
  default = 7
}

variable "multi_az" {
  type    = bool
  default = false
}

variable "tags" {
  type    = map(string)
  default = {}
}
