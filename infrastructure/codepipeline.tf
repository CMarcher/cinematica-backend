# API pipeline #

resource "aws_codepipeline" "api_pipeline" {
    name     = "api-pipeline"
    role_arn = aws_iam_role.api_codepipeline_role.arn
    
    artifact_store {
        location = aws_s3_bucket.api_pipeline_artifact_bucket.bucket
        type     = "S3"
    }
    
    stage {
        name = "Source"
        
        action {
            name = "Source"
            category = "Source"
            owner    = "AWS"
            provider = "S3"
            version  = "1"
            
            configuration = {
                S3Bucket = aws_s3_bucket.api_lambda_bucket.bucket
                S3ObjectKey = "Cinematica.API"
                PollForSourceChanges = false
            }
            
            output_artifacts = ["source_output"]
        }
    }
    
    stage {
        name = "Build"
        
        action {
            name     = "Build"
            category = "Build"
            owner    = "AWS"
            provider = "CodeBuild"
            version  = "1"
            
            configuration = {
                ProjectName = aws_codebuild_project.api_build.name
            }

            input_artifacts = ["source_output"]
            output_artifacts = ["build_output"]
        }
    }
}

resource "aws_iam_role" "api_codepipeline_role" {
    name = "APICodePipelineRole"
    assume_role_policy = data.aws_iam_policy_document.codepipeline_trust_policy.json
}

data "aws_iam_policy_document" "codepipeline_trust_policy" {
    statement {
        effect = "Allow"

        principals {
            type = "Service"
            identifiers = ["codepipeline.amazonaws.com"]
        }

        actions = ["sts:AssumeRole"]
    }
}

resource "aws_iam_role_policy_attachment" "api_pipeline_policy_attachment" {
    policy_arn = aws_iam_policy.api_pipeline_policy.arn
    role       = aws_iam_role.api_codepipeline_role.name
}

resource "aws_iam_policy" "api_pipeline_policy" {
    name = "APICodePipelinePermissions"
    policy = data.aws_iam_policy_document.api_pipeline_policy_document.json
}

data "aws_iam_policy_document" "api_pipeline_policy_document" {
    statement {
        effect = "Allow"
        
        actions = [
            "s3:GetObject",
            "s3:GetObjectVersion",
            "s3:GetBucketVersioning"
        ]
        
        resources = [
            aws_s3_bucket.api_lambda_bucket.arn,
            "${aws_s3_bucket.api_lambda_bucket.arn}/*"
        ]
    }

    statement {
        effect = "Allow"

        actions = [
            "s3:GetObject",
            "s3:GetObjectVersion",
            "s3:GetBucketVersioning",
            "s3:PutObjectAcl",
            "s3:PutObject",
        ]

        resources = [
            aws_s3_bucket.api_pipeline_artifact_bucket.arn,
            "${aws_s3_bucket.api_pipeline_artifact_bucket.arn}/*"
        ]
    }

    statement {
        effect = "Allow"

        actions = [
            "codebuild:BatchGetBuilds",
            "codebuild:StartBuild",
        ]

        resources = [aws_codebuild_project.api_build.arn]
    }
}

# Front-end pipeline #

resource "aws_codepipeline" "web_app_pipeline" {
    name     = "web-app-pipeline"
    role_arn = aws_iam_role.web_app_codepipeline_role.arn

    artifact_store {
        location = aws_s3_bucket.web_app_pipeline_artifact_bucket.bucket
        type     = "S3"
    }

    stage {
        name = "Source"

        action {
            name = "Source"
            category = "Source"
            owner    = "AWS"
            provider = "S3"
            version  = "1"

            configuration = {
                S3Bucket = aws_s3_bucket.web_app_bucket.bucket
                S3ObjectKey = "webapp.zip"
                PollForSourceChanges = false
            }

            output_artifacts = ["source_output"]
        }
    }

    stage {
        name = "Build"

        action {
            name     = "Build"
            category = "Build"
            owner    = "AWS"
            provider = "CodeBuild"
            version  = "1"

            configuration = {
                ProjectName = aws_codebuild_project.web_app_build.name
            }

            input_artifacts = ["source_output"]
            output_artifacts = ["build_output"]
        }
    }
    
    stage {
        name = "Deploy"
        
        action {
            name     = "Deploy"
            category = "Deploy"
            owner    = "AWS"
            provider = "ECS"
            version  = "1"
            
            configuration = {
                ClusterName = aws_ecs_cluster.web_app_cluster.name
                ServiceName = aws_ecs_service.web_app_service.name
            }
            
            input_artifacts = ["build_output"]
        }
    }
}

resource "aws_iam_role" "web_app_codepipeline_role" {
    name = "WebAppCodePipelineRole"
    assume_role_policy = data.aws_iam_policy_document.codepipeline_trust_policy.json
}

resource "aws_iam_role_policy_attachment" "web_app_pipeline_policy_attachment" {
    policy_arn = aws_iam_policy.web_app_pipeline_policy.arn
    role       = aws_iam_role.web_app_codepipeline_role.name
}

resource "aws_iam_policy" "web_app_pipeline_policy" {
    name = "WebAppCodePipelinePermissions"
    policy = data.aws_iam_policy_document.web_app_pipeline_policy_document.json
}

data "aws_iam_policy_document" "web_app_pipeline_policy_document" {
    statement {
        effect = "Allow"

        actions = [
            "s3:GetObject",
            "s3:GetObjectVersion",
            "s3:GetBucketVersioning"
        ]

        resources = [
            aws_s3_bucket.web_app_bucket.arn,
            "${aws_s3_bucket.web_app_bucket.arn}/*"
        ]
    }

    statement {
        effect = "Allow"

        actions = [
            "s3:GetObject",
            "s3:GetObjectVersion",
            "s3:GetBucketVersioning",
            "s3:PutObjectAcl",
            "s3:PutObject",
        ]

        resources = [
            aws_s3_bucket.web_app_pipeline_artifact_bucket.arn,
            "${aws_s3_bucket.web_app_pipeline_artifact_bucket.arn}/*"
        ]
    }

    statement {
        effect = "Allow"

        actions = [
            "codebuild:BatchGetBuilds",
            "codebuild:StartBuild",
        ]

        resources = [aws_codebuild_project.web_app_build.arn]
    }
}
