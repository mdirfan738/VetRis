# ---------------------------------------------------------------------------
# VetRIS-Scaffold - dev environment root module.
# Wires together: network -> security -> storage -> database -> backend ->
# frontend -> iam-oidc.
# ---------------------------------------------------------------------------

locals {
  tags = {
    Project     = "VetRIS"
    Environment = var.environment
  }
}

module "network" {
  source      = "../../modules/network"
  project     = "VetRIS"
  environment = var.environment
  azs         = var.azs
  tags        = local.tags
}

module "security" {
  source      = "../../modules/security"
  project     = "VetRIS"
  environment = var.environment
  tags        = local.tags

  providers = {
    aws            = aws
    aws.us_east_1  = aws.us_east_1
  }
}

module "storage" {
  source      = "../../modules/storage"
  project     = "VetRIS"
  environment = var.environment
  kms_key_arn = module.security.kms_key_arn
  tags        = local.tags
}

module "database" {
  source                      = "../../modules/database"
  project                     = "VetRIS"
  environment                 = var.environment
  vpc_id                      = module.network.vpc_id
  private_subnet_ids          = module.network.private_subnet_ids
  database_security_group_id  = module.network.database_security_group_id
  kms_key_arn                 = module.security.kms_key_arn
  db_master_password          = module.security.db_master_password
  instance_class               = var.db_instance_class
  multi_az                    = var.multi_az
  tags                         = local.tags
}

module "backend" {
  source                     = "../../modules/backend"
  project                    = "VetRIS"
  environment                = var.environment
  vpc_id                     = module.network.vpc_id
  public_subnet_ids          = module.network.public_subnet_ids
  private_subnet_ids         = module.network.private_subnet_ids
  backend_security_group_id  = module.network.backend_security_group_id
  kms_key_arn                = module.security.kms_key_arn
  db_endpoint                = module.database.db_address
  db_name                    = module.database.db_name
  db_master_username         = "Admin"
  db_master_secret_arn       = module.security.db_master_secret_arn
  dicom_active_bucket_name   = module.storage.dicom_active_bucket_name
  dicom_active_bucket_arn    = module.storage.dicom_active_bucket_arn
  allowed_origin             = var.backend_allowed_origin
  log_group_name             = module.security.backend_log_group_name
  api_domain_name            = var.api_domain_name
  route53_zone_id            = var.route53_zone_id
  tags                       = local.tags
}

module "frontend" {
  source                   = "../../modules/frontend"
  project                  = "VetRIS"
  environment              = var.environment
  domain_name              = var.domain_name
  route53_zone_id          = var.route53_zone_id
  waf_web_acl_arn          = module.security.waf_web_acl_arn
  kms_key_arn              = module.security.kms_key_arn
  access_logs_bucket_name  = module.storage.access_logs_bucket_name
  tags                     = local.tags

  providers = {
    aws           = aws
    aws.us_east_1 = aws.us_east_1
  }
}

module "iam_oidc" {
  source                       = "../../modules/iam-oidc"
  project                      = "VetRIS"
  environment                  = var.environment
  github_org                   = var.github_org
  github_repo                  = var.github_repo
  ecr_repository_arn           = "arn:aws:ecr:${var.aws_region}:${data.aws_caller_identity.current.account_id}:repository/${lower("VetRIS")}-${var.environment}-backend"
  frontend_bucket_arn          = "arn:aws:s3:::${module.frontend.frontend_bucket_name}"
  cloudfront_distribution_arn  = "arn:aws:cloudfront::${data.aws_caller_identity.current.account_id}:distribution/${module.frontend.cloudfront_distribution_id}"
  ecs_cluster_arn              = module.backend.ecs_cluster_arn
  ecs_service_arn              = module.backend.ecs_service_arn
  ecs_execution_role_arn       = module.backend.ecs_execution_role_arn
  ecs_task_role_arn            = module.backend.ecs_task_role_arn
  state_bucket_arn             = var.tf_state_bucket_arn
  #state_lock_table_arn         = var.tf_lock_table_arn
  kms_key_arn                  = module.security.kms_key_arn
  tags                         = local.tags
}

data "aws_caller_identity" "current" {}
