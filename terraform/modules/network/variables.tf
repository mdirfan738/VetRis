variable "project" {
  type        = string
  description = "Project tag value"
  default     = "VetRIS"
}

variable "environment" {
  type        = string
  description = "Environment name, e.g. dev, staging, prod"
}

variable "vpc_cidr" {
  type        = string
  default     = "10.20.0.0/16"
}

variable "public_subnet_cidrs" {
  type    = list(string)
  default = ["10.20.0.0/24", "10.20.1.0/24"]
}

variable "private_subnet_cidrs" {
  type    = list(string)
  default = ["10.20.10.0/24", "10.20.11.0/24"]
}

variable "azs" {
  type        = list(string)
  description = "Availability zones to spread subnets across"
}

variable "tags" {
  type    = map(string)
  default = {}
}
