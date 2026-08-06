variable "project" {
  type    = string
  default = "VetRIS"
}

variable "environment" {
  type = string
}

variable "domain_name" {
  type        = string
  description = "Full domain the frontend will be served from, e.g. app.{{YOUR_DOMAIN}}"
}

variable "route53_zone_id" {
  type        = string
  description = "Existing Route53 hosted zone id for {{YOUR_DOMAIN}}"
}



variable "waf_web_acl_arn" {
  type = string
}

variable "kms_key_arn" {
  type = string
}

variable "access_logs_bucket_name" {
  type = string
}

variable "tags" {
  type    = map(string)
  default = {}
}
