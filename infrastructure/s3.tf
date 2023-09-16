# API source bucket #

resource "aws_s3_bucket" "api_lambda_bucket" {
    bucket = "cinematica-api-lambda-source"
}

resource "aws_s3_bucket_versioning" "api_lambda_bucket_versioning" {
    bucket = aws_s3_bucket.api_lambda_bucket.bucket
}

# Media bucket #

resource "aws_s3_bucket" "media_bucket" {
    bucket = "cinematica-media"
}

resource "aws_s3_object" "movies_directory" {
    bucket = aws_s3_bucket.media_bucket.id
    key    = "movies/"
    content_type = "application/x-directory"
}

resource "aws_s3_object" "posts_directory" {
    bucket = aws_s3_bucket.media_bucket.id
    key    = "posts/"
    content_type = "application/x-directory"
}

resource "aws_s3_object" "users_directory" {
    bucket = aws_s3_bucket.media_bucket.id
    key    = "users/"
    content_type = "application/x-directory"
}

resource "aws_s3_bucket_policy" "cinematica_media_policy" {
    bucket = aws_s3_bucket.media_bucket.id
    policy = data.aws_iam_policy_document.cinematica_media_policy_document.json
}

data "aws_iam_policy_document" "cinematica_media_policy_document" {
    statement {
        actions = ["s3:GetObject"]
        resources = ["${aws_s3_bucket.media_bucket.arn}/*"]

        principals {
            type = "AWS"
            identifiers = [aws_cloudfront_origin_access_identity.cloudfront_s3_identity.iam_arn]
        }
    }
}

# API Pipeline artifact bucket #

resource "aws_s3_bucket" "api_pipeline_artifact_bucket" {
    bucket = "cinematica-api-codepipeline-artifact-store"
}