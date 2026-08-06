output "kms_key_arn" {
  value = aws_kms_key.vetris.arn
}

output "kms_key_id" {
  value = aws_kms_key.vetris.key_id
}

output "db_master_secret_arn" {
  value = aws_secretsmanager_secret.db_master.arn
}

output "db_master_password" {
  value     = random_password.db_master.result
  sensitive = true
}

output "waf_web_acl_arn" {
  value = aws_wafv2_web_acl.cloudfront.arn
}

output "backend_log_group_name" {
  value = aws_cloudwatch_log_group.backend.name
}
