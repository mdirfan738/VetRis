# ---------------------------------------------------------------------------
# Network module: VPC, 2 public + 2 private subnets, single NAT Gateway,
# security groups for backend compute and RDS.
#
# HIPAA note: private subnets have no route to the internet except via NAT
# (outbound only), so RDS and any backend compute placed there are not
# directly reachable from the public internet.
# ---------------------------------------------------------------------------

locals {
  common_tags = merge(var.tags, {
    Project     = var.project
    Environment = var.environment
    ManagedBy   = "terraform"
  })
}

resource "aws_vpc" "this" {
  cidr_block           = var.vpc_cidr
  enable_dns_support   = true
  enable_dns_hostnames = true

  tags = merge(local.common_tags, { Name = "${var.project}-${var.environment}-vpc" })
}

resource "aws_internet_gateway" "this" {
  vpc_id = aws_vpc.this.id
  tags   = merge(local.common_tags, { Name = "${var.project}-${var.environment}-igw" })
}

resource "aws_subnet" "public" {
  count                   = length(var.public_subnet_cidrs)
  vpc_id                  = aws_vpc.this.id
  cidr_block              = var.public_subnet_cidrs[count.index]
  availability_zone       = var.azs[count.index]
  map_public_ip_on_launch = true

  tags = merge(local.common_tags, {
    Name = "${var.project}-${var.environment}-public-${count.index + 1}"
    Tier = "public"
  })
}

resource "aws_subnet" "private" {
  count             = length(var.private_subnet_cidrs)
  vpc_id            = aws_vpc.this.id
  cidr_block        = var.private_subnet_cidrs[count.index]
  availability_zone = var.azs[count.index]

  tags = merge(local.common_tags, {
    Name = "${var.project}-${var.environment}-private-${count.index + 1}"
    Tier = "private"
  })
}

# Single NAT Gateway for cost efficiency in dev/staging.
# For prod, consider one NAT GW per AZ for higher availability (see README scaling plan).
resource "aws_eip" "nat" {
  domain = "vpc"
  tags   = merge(local.common_tags, { Name = "${var.project}-${var.environment}-nat-eip" })
}

resource "aws_nat_gateway" "this" {
  allocation_id = aws_eip.nat.id
  subnet_id     = aws_subnet.public[0].id
  tags          = merge(local.common_tags, { Name = "${var.project}-${var.environment}-nat" })
  depends_on    = [aws_internet_gateway.this]
}

resource "aws_route_table" "public" {
  vpc_id = aws_vpc.this.id
  tags   = merge(local.common_tags, { Name = "${var.project}-${var.environment}-public-rt" })

  route {
    cidr_block = "0.0.0.0/0"
    gateway_id = aws_internet_gateway.this.id
  }
}

resource "aws_route_table_association" "public" {
  count          = length(aws_subnet.public)
  subnet_id      = aws_subnet.public[count.index].id
  route_table_id = aws_route_table.public.id
}

resource "aws_route_table" "private" {
  vpc_id = aws_vpc.this.id
  tags   = merge(local.common_tags, { Name = "${var.project}-${var.environment}-private-rt" })

  route {
    cidr_block     = "0.0.0.0/0"
    nat_gateway_id = aws_nat_gateway.this.id
  }
}

resource "aws_route_table_association" "private" {
  count          = length(aws_subnet.private)
  subnet_id      = aws_subnet.private[count.index].id
  route_table_id = aws_route_table.private.id
}

# ---------------------------------------------------------------------------
# Security Groups
# ---------------------------------------------------------------------------

# Backend compute (App Runner VPC connector / ECS tasks) egress-only by default,
# ingress restricted to what's needed (App Runner reaches it via VPC connector,
# so no inbound rule from the internet is required here).
resource "aws_security_group" "backend" {
  name_prefix = "${var.project}-${var.environment}-backend-"
  description = "SG for VetRIS backend compute (App Runner connector / ECS tasks)"
  vpc_id      = aws_vpc.this.id

  egress {
    description = "Allow all outbound (S3, RDS, ECR, CloudWatch, etc via NAT/VPC endpoints)"
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = merge(local.common_tags, { Name = "${var.project}-${var.environment}-backend-sg" })

  lifecycle {
    create_before_destroy = true
  }
}

# RDS: only allow inbound MySQL (3306) from the backend security group.
resource "aws_security_group" "database" {
  name_prefix = "${var.project}-${var.environment}-db-"
  description = "SG for VetRIS RDS MySQL - only reachable from backend SG"
  vpc_id      = aws_vpc.this.id

  ingress {
    description     = "MySQL from backend"
    from_port       = 3306
    to_port         = 3306
    protocol        = "tcp"
    security_groups = [aws_security_group.backend.id]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = merge(local.common_tags, { Name = "${var.project}-${var.environment}-db-sg" })

  lifecycle {
    create_before_destroy = true
  }
}
