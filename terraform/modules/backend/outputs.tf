output "ecr_repository_url" {
  value = aws_ecr_repository.backend.repository_url
}

output "app_runner_service_url" {
  value = aws_apprunner_service.backend.service_url
}

output "app_runner_service_arn" {
  value = aws_apprunner_service.backend.arn
}

output "apprunner_task_role_arn" {
  value = aws_iam_role.apprunner_task.arn
}
