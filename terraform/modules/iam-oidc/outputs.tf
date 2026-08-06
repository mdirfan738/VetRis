output "infra_cicd_role_arn" {
  value = aws_iam_role.infra_cicd.arn
}

output "backend_cicd_role_arn" {
  value = aws_iam_role.backend_cicd.arn
}

output "frontend_cicd_role_arn" {
  value = aws_iam_role.frontend_cicd.arn
}

output "github_oidc_provider_arn" {
  value = aws_iam_openid_connect_provider.github.arn
}
