resource "aws_codebuild_project" "api_build" {
    name         = "api-build"
    service_role = aws_iam_role.api_codebuild_role.arn
    build_timeout = 5
    
    artifacts {
        type = "NO_ARTIFACTS"
    }
    
    environment {
        compute_type = "BUILD_GENERAL1_SMALL"
        image        = "aws/codebuild/amazonlinux2-x86_64-standard:5.0"
        type         = "LINUX_CONTAINER"
        image_pull_credentials_type = "CODEBUILD"
        
        environment_variable {
            name  = "LAMBDA_FUNCTION_NAME"
            value = aws_lambda_function.cinematica_api_lambda.function_name
        }
    }
    
    source {
        type = "NO_SOURCE"
        buildspec = yamlencode({
            version = "0.2"
            phases = {
                build = {
                    commands = [
                        "aws lambda update-function-code $${LAMBDA_FUNCTION_NAME}"
                    ]
                }
            }
        })
    }
    
    logs_config {
        cloudwatch_logs {
            group_name = aws_cloudwatch_log_group.api_codebuild_log_group.name
            status = "ENABLED"
        }
    }
}

resource "aws_iam_role" "api_codebuild_role" {
    name = "APICodeBuildServiceRole"
    assume_role_policy = data.aws_iam_policy_document.codebuild_trust_policy.json
}

data "aws_iam_policy_document" "codebuild_trust_policy" {
    statement {
        effect = "Allow"

        principals {
            type = "Service"
            identifiers = ["codebuild.amazonaws.com"]
        }

        actions = ["sts:AssumeRole"]
    }
}

resource "aws_iam_role_policy_attachment" "api_codebuild_policy_attachment" {
    policy_arn = aws_iam_policy.api_codebuild_policy.arn
    role       = aws_iam_role.api_codebuild_role.name
}

resource "aws_iam_policy" "api_codebuild_policy" {
    name = "APICodeBuildPermissions"
    policy = data.aws_iam_policy_document.api_codebuild_policy_document.json
}

data "aws_iam_policy_document" "api_codebuild_policy_document" {
    statement {
        sid = "AllowAPILambdaUpdateAccess"
        effect = "Allow"
        actions = ["lambda:UpdateFunctionCode"]
        resources = [aws_lambda_function.cinematica_api_lambda.arn]
    }
    
    statement {
        sid = "AllowAPICodeBuildLoggingAccess"
        effect = "Allow"
        
        actions = [
            "logs:CreateLogGroup",
            "logs:CreateLogStream",
            "logs:PutLogEvents",
            "logs:FilterLogEvents",
            "logs:GetLogEvents"
        ]
        
        resources = ["${aws_cloudwatch_log_group.api_codebuild_log_group.arn}:*"]
    }
    
    statement {
        sid = "AllowCodeBuildToAccessArtifactsPolicy"
        effect = "Allow"
        
        actions = [
            "s3:GetObject",
            "s3:GetObjectVersion",
            "s3:GetBucketVersioning",
            "s3:GetBucketAcl", 
            "s3:GetBucketLocation"
        ]
        
        resources = [
            aws_s3_bucket.api_pipeline_artifact_bucket.arn,
            "${aws_s3_bucket.api_pipeline_artifact_bucket.arn}:*"
        ]
    }
}

resource "aws_cloudwatch_log_group" "api_codebuild_log_group" {
    name = "/aws/codebuild/api"
}