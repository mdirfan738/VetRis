output "ecr_repository_url" {
  value = aws_ecr_repository.backend.repository_url
}

output "ecs_cluster_name" {
  value = aws_ecs_cluster.this.name
}

output "ecs_cluster_arn" {
  value = aws_ecs_cluster.this.arn
}

output "ecs_execution_role_arn" {
  value = aws_iam_role.ecs_execution.arn
}

output "ecs_task_role_arn" {
  value = aws_iam_role.ecs_task.arn
}

output "ecs_service_name" {
  value = aws_ecs_service.backend.name
}

output "ecs_service_arn" {
  value = aws_ecs_service.backend.id
}

output "ecs_task_definition_family" {
  value = aws_ecs_task_definition.backend.family
}

output "alb_dns_name" {
  value = aws_lb.backend.dns_name
}

output "alb_arn" {
  value = aws_lb.backend.arn
}

output "target_group_arn" {
  value = aws_lb_target_group.backend.arn
}

output "api_url" {
  value       = "https://${var.api_domain_name}"
  description = "The real, Route53-backed URL — prefer this over alb_dns_name for anything user-facing"
}

output "log_group_name" {
  value = local.log_group_name
}
