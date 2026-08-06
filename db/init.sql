-- VetRIS-Scaffold local dev bootstrap.
-- In AWS, RDS is provisioned by Terraform and EF Core migrations
-- (Database.Migrate() in Program.cs) create/update this schema instead.

CREATE DATABASE IF NOT EXISTS vetris
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

USE vetris;

CREATE TABLE IF NOT EXISTS Studies (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  PatientId VARCHAR(128) NOT NULL,
  StudyDate DATETIME(6) NOT NULL,
  S3Key VARCHAR(512) NOT NULL,
  Status VARCHAR(64) NOT NULL DEFAULT 'Received',
  CreatedAtUtc DATETIME(6) NOT NULL,
  INDEX idx_patient_id (PatientId),
  INDEX idx_status (Status)
) ENGINE=InnoDB;
