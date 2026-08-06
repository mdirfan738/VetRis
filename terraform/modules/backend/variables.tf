variable "project" {
  type    = string
  default = "VetRIS"
}

variable "environment" {
  type = string
}

variable "private_subnet_ids" {
  type = list(string)
}

variable "backend_security_group_id" {
  type = string
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
  type = string
}

variable "dicom_active_bucket_name" {
  type = string
}

variable "dicom_active_bucket_arn" {
  type = string
}

variable "allowed_origin" {
  type        = string
  description = "CORS origin allowed to call the API, e.g. https://app.example.com"
}

variable "cpu" {
  type    = string
  default = "1 vCPU"
}

variable "memory" {
  type    = string
  default = "2 GB"
}

variable "min_size" {
  type    = number
  default = 1
}

variable "max_size" {
  type    = number
  description = "Max concurrent App Runner instances (scaling plan lever)"
  default = 5
}

variable "log_group_name" {
  type = string
}

variable "tags" {
  type    = map(string)
  default = {}
}
