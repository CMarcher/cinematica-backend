locals {
  account_id = data.aws_caller_identity.current.account_id
  s3_origin_id = "cinematica-origin"
  database_snapshot_identifier = "cinematica-database-latest"
}