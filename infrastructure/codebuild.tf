# API CodeBuild project #

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
                        "ls",
                        "aws lambda update-function-code --function-name $${LAMBDA_FUNCTION_NAME} --s3-bucket ${aws_s3_bucket.api_lambda_bucket.bucket} --s3-key Cinematica.API"
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
            "${aws_s3_bucket.api_pipeline_artifact_bucket.arn}/*",
            aws_s3_bucket.api_lambda_bucket.arn,
            "${aws_s3_bucket.api_lambda_bucket.arn}/*"
        ]
    }
}

resource "aws_cloudwatch_log_group" "api_codebuild_log_group" {
    name = "/aws/codebuild/api"
}

# Front-end CodeBuild project #

resource "aws_codebuild_project" "web_app_build" {
    name         = "web-app-build"
    service_role = aws_iam_role.web_app_codebuild_role.arn
    build_timeout = 5

    artifacts {
        type = "CODEPIPELINE"
        artifact_identifier = "imagedefinitions.json"
    }

    environment {
        compute_type = "BUILD_GENERAL1_SMALL"
        image        = "aws/codebuild/amazonlinux2-x86_64-standard:5.0"
        type         = "LINUX_CONTAINER"
        image_pull_credentials_type = "CODEBUILD"
        privileged_mode = true

        environment_variable {
            name  = "LAMBDA_FUNCTION_NAME"
            value = aws_lambda_function.cinematica_api_lambda.function_name
        }
    }

    source {
        type = "CODEPIPELINE"
        buildspec = yamlencode({
            version = "0.2"
            phases = {
                pre_build = {
                    commands = [
                        "echo Files in directory (before):",
                        "ls",
                        "echo Unzipping front-end source code...",
                        "unzip webapp.zip",
                        "echo Removing zip file...",
                        "rm webapp.zip",
                        "echo Files in directory (after):",
                        "ls",
                        
                        "REPOSITORY_URI=${aws_ecrpublic_repository.web_app_image_repository.repository_uri}/front-end",
                        "echo Logging in to Amazon ECR...",
                        "aws ecr-public get-login-password --region us-east-1 | docker login --username AWS --password-stdin public.ecr.aws",
                    ]
                }
                
                build = {
                    commands = [
                        "echo Building front-end Docker image...",
                        "docker build -t $REPOSITORY_URI:latest"
                    ]
                }
                
                post_build = {
                    commands = [
                        "echo Build completed on `date`",
                        "echo Pushing Docker image...",
                        "docker push $REPOSITORY_URI:latest",
                        "echo Writing image definitions file...",
                        "printf '[{\"name\" : \"cinematica-front-end\", \"imageUri\" : \"%s\"}]' $REPOSITORY_URI:latest > imagedefinitions.json"
                    ]
                }
            }
            artifacts = {
                files = "imagedefinitions.json"
            }
        })
    }

    logs_config {
        cloudwatch_logs {
            group_name = aws_cloudwatch_log_group.web_app_codebuild_log_group.name
            status = "ENABLED"
        }
    }
}

resource "aws_cloudwatch_log_group" "web_app_codebuild_log_group" {
    name = "/aws/codebuild/webapp"
    retention_in_days = 7
}

resource "aws_iam_role" "web_app_codebuild_role" {
    name = "WebAppCodeBuildServiceRole"
    assume_role_policy = data.aws_iam_policy_document.codebuild_trust_policy.json
}

resource "aws_iam_role_policy_attachment" "web_app_codebuild_policy_attachment" {
    policy_arn = aws_iam_policy.web_app_codebuild_policy.arn
    role       = aws_iam_role.web_app_codebuild_role.name
}

resource "aws_iam_policy" "web_app_codebuild_policy" {
    name = "WebAppCodeBuildPermissions"
    policy = data.aws_iam_policy_document.web_app_codebuild_policy_document.json
}

data "aws_iam_policy_document" "web_app_codebuild_policy_document" {
    statement {
        sid = "AllowWebAppCodeBuildLoggingAccess"
        effect = "Allow"

        actions = [
            "logs:CreateLogGroup",
            "logs:CreateLogStream",
            "logs:PutLogEvents",
            "logs:FilterLogEvents",
            "logs:GetLogEvents"
        ]

        resources = ["${aws_cloudwatch_log_group.web_app_codebuild_log_group.arn}:*"]
    }

    statement {
        sid = "AllowCodeBuildToAccessArtifactsPolicy"
        effect = "Allow"

        actions = [
            "s3:GetObject",
            "s3:GetObjectVersion",
            "s3:GetBucketVersioning",
            "s3:GetBucketAcl",
            "s3:GetBucketLocation",
            "s3:PutObject"
        ]

        resources = [
            aws_s3_bucket.web_app_pipeline_artifact_bucket.arn,
            "${aws_s3_bucket.web_app_pipeline_artifact_bucket.arn}/*"
        ]
    }
    
    statement {
        sid = "AllowCodeBuildToAccessCinematicaECR"
        effect = "Allow"
        
        actions = [
            "ecr:GetAuthorizationToken",
            "ecr:BatchCheckLayerAvailability",
            "ecr:GetDownloadUrlForLayer",
            "ecr:GetRepositoryPolicy",
            "ecr:DescribeRepositories",
            "ecr:ListImages",
            "ecr:DescribeImages",
            "ecr:BatchGetImage",
            "ecr:GetLifecyclePolicy",
            "ecr:GetLifecyclePolicyPreview",
            "ecr:ListTagsForResource",
            "ecr:DescribeImageScanFindings",
            "ecr:InitiateLayerUpload",
            "ecr:UploadLayerPart",
            "ecr:CompleteLayerUpload",
            "ecr:PutImage"
        ]
        
        resources = [aws_ecrpublic_repository.web_app_image_repository.arn]
    }
}