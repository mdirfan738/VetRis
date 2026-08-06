# ---------------------------------------------------------------------------
# Database module: RDS MySQL 8, private subnets only, encrypted with the
# shared CMK, automated backups (7 day retention by default), TLS-required
# custom parameter group.
# ---------------------------------------------------------------------------

locals {
  common_tags = merge(var.tags, {
    Project     = var.project
    Environment = var.environment
    ManagedBy   = "terraform"
  })
}

resource "aws_db_subnet_group" "this" {
  name       = "${lower(var.project)}-${var.environment}-db-subnets"
  subnet_ids = var.private_subnet_ids

  tags = local.common_tags
}

# Enforce TLS for all client connections (HIPAA: encryption in transit)
resource "aws_db_parameter_group" "mysql8" {
  name   = "${lower(var.project)}-${var.environment}-mysql8"
  family = "mysql8.0"

  parameter {
    name  = "require_secure_transport"
    value = "1"
  }

  tags = local.common_tags
}

resource "aws_db_instance" "this" {
  identifier     = "${lower(var.project)}-${var.environment}-mysql"
  engine         = "mysql"
  engine_version = "8.0"

  instance_class    = var.instance_class
  allocated_storage = var.allocated_storage_gb
  storage_type      = "gp3"
  storage_encrypted = true
  kms_key_id        = var.kms_key_arn

  db_name  = "vetris"
  username = var.db_master_username
  password = var.db_master_password
  port     = 3306

  db_subnet_group_name   = aws_db_subnet_group.this.name
  vpc_security_group_ids = [var.database_security_group_id]
  parameter_group_name   = aws_db_parameter_group.mysql8.name

  publicly_accessible = false
  multi_az             = var.multi_az

  backup_retention_period = var.backup_retention_days
  backup_window            = "03:00-04:00"
  maintenance_window       = "mon:04:30-mon:05:30"

  # HIPAA: audit / logging
  enabled_cloudwatch_logs_exports = ["error", "general", "slowquery"]

  deletion_protection      = true
  skip_final_snapshot      = false
  final_snapshot_identifier = "${lower(var.project)}-${var.environment}-final-snapshot"
  copy_tags_to_snapshot     = true

  auto_minor_version_upgrade = true

  tags = merge(local.common_tags, { Name = "${var.project}-${var.environment}-mysql" })
}
