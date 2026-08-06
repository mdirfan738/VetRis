variable "project" {
  type    = string
  default = "VetRIS"
}

variable "environment" {
  type = string
}

variable "cloudfront_distribution_arn" {
  type        = string
  description = "ARN of the CloudFront distribution to attach WAF to (set after frontend module creates it; use a 2-phase apply or pass null and attach separately)"
  default     = null
}

variable "tags" {
  type    = map(string)
  default = {}
}
