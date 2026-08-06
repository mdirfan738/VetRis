output "dicom_active_bucket_name" {
  value = aws_s3_bucket.dicom_active.bucket
}

output "dicom_active_bucket_arn" {
  value = aws_s3_bucket.dicom_active.arn
}

output "dicom_archive_bucket_name" {
  value = aws_s3_bucket.dicom_archive.bucket
}

output "dicom_archive_bucket_arn" {
  value = aws_s3_bucket.dicom_archive.arn
}

output "access_logs_bucket_name" {
  value = aws_s3_bucket.logs.bucket
}
