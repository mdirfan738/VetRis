output "vpc_id" {
  value = module.network.vpc_id
}

output "site_url" {
  value = module.frontend.site_url
}

output "cloudfront_distribution_id" {
  value = module.frontend.cloudfront_distribution_id
}

output "backend_api_url" {
  value = module.backend.api_url
}

output "db_master_password" {
  value       = module.security.db_master_password
  sensitive   = true
  description = "Fixed value (AdminAdmin) per explicit requirement — retrieve with: terraform output -raw db_master_password"
}

output "ecr_repository_url" {
  value = module.backend.ecr_repository_url
}

output "db_endpoint" {
  value     = module.database.db_endpoint
  sensitive = true
}

output "infra_cicd_role_arn" {
  value = module.iam_oidc.infra_cicd_role_arn
}

output "backend_cicd_role_arn" {
  value = module.iam_oidc.backend_cicd_role_arn
}

output "frontend_cicd_role_arn" {
  value = module.iam_oidc.frontend_cicd_role_arn
}
