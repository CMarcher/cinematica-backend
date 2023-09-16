resource "aws_cloudwatch_event_rule" "s3_api_pipeline_trigger" {
    name = "APIPipelineS3Trigger"
    description = "Start the API Pipeline when the S3-stored source package is updated."
    role_arn = aws_iam_role.api_pipeline_trigger.arn

    event_pattern = jsonencode({
        source = ["aws.s3"]
        detail-type = ["Object Created"]
        detail = {
            bucket = {
                name = [aws_s3_bucket.api_lambda_bucket.bucket]
            }
        }
    })
}

resource "aws_cloudwatch_event_target" "api_codepipeline_target" {
    arn  = aws_codepipeline.api_pipeline.arn
    rule = aws_cloudwatch_event_rule.s3_api_pipeline_trigger.name
    target_id = aws_codepipeline.api_pipeline.name
}

resource "aws_iam_role" "api_pipeline_trigger" {
    name = "APIPipelineTriggerRole"
    assume_role_policy = data.aws_iam_policy_document.events_trust_policy.json
}

data "aws_iam_policy_document" "events_trust_policy" {
    statement {
        effect = "Allow"

        principals {
            type = "Service"
            identifiers = ["events.amazonaws.com"]
        }

        actions = ["sts:AssumeRole"]
    }
}

resource "aws_iam_role_policy_attachment" "api_pipeline_trigger_policy_attachment" {
    policy_arn = aws_iam_policy.api_pipeline_trigger_policy.arn
    role       = aws_iam_role.api_pipeline_trigger.name
}

resource "aws_iam_policy" "api_pipeline_trigger_policy" {
    name = "EventBridgeAPIPipelineAccess"
    policy = data.aws_iam_policy_document.api_pipeline_trigger_policy_document.json
}

data "aws_iam_policy_document" "api_pipeline_trigger_policy_document" {
    statement {
        effect = "Allow"
        actions = ["codepipeline:StartPipelineExecution"]
        resources = [aws_codepipeline.api_pipeline.arn]
    }
}