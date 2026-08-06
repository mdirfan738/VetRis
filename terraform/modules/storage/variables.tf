variable "project" {
  type    = string
  default = "VetRIS"
}

variable "environment" {
  type = string
}

variable "kms_key_arn" {
  type        = string
  description = "KMS key ARN used for SSE-KMS on the DICOM buckets"
}

variable "tags" {
  type    = map(string)
  default = {}
}
