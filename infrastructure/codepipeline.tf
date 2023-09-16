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