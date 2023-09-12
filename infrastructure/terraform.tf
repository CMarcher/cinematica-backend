terraform {
    required_providers {
        aws = {
            source = "hashicorp/aws"
            version = "5.13.1"
        }

        cloudflare = {
            source = "cloudflare/cloudflare"
            version = "4.14.0"
        }
    }
}

provider "aws" {
    region = "ap-southeast-2"
}

provider "aws" {
    alias = "us_east"
    region = "us-east-1"
}

provider "cloudflare" { }

data "aws_region" "api_gateway_region" { provider = aws.us_east }
data "aws_caller_identity" "current" {}

locals {
    account_id = data.aws_caller_identity.current.account_id
}

variable "region" { default = "ap-southeast-2" }
variable "api_domain_name" { default = "api.cinematica.social" }
variable "zone_id" { }

# # # # # #
#   S3    #
# # # # # #

resource "aws_s3_bucket" "api_lambda_bucket" {
    bucket = "cinematica-api-lambda-source"
}

# # # # # #
# Lambda  #
# # # # # #

variable "cinematica_lambda_function_name" {
    default = "cinematica-api"
}

resource "aws_lambda_function" "cinematica_api_lambda" {
    function_name = var.cinematica_lambda_function_name
    handler = "Cinematica.API::Cinematica.API.LambdaEntryPoint::FunctionHandlerAsync"
    runtime = "dotnet6"
    s3_bucket = aws_s3_bucket.api_lambda_bucket.bucket
    s3_key = "Cinematica.API"
    role = aws_iam_role.cinematica_api_lambda_role.arn
    memory_size = 1024
    package_type = "Zip"
    timeout = 30

    depends_on = [
        aws_iam_role_policy_attachment.cinematica_api_lambda_log_policy_attachment,
        aws_cloudwatch_log_group.cinematica_api_lambda_log_group
    ]
}

resource "aws_iam_role" "cinematica_api_lambda_role" {
    name = "cinematica-api-lambda-role"
    assume_role_policy = data.aws_iam_policy_document.lambda_assume_role.json
}

data "aws_iam_policy_document" "lambda_assume_role" {
    statement {
        effect = "Allow"

        principals {
            type = "Service"
            identifiers = ["lambda.amazonaws.com"]
        }

        actions = ["sts:AssumeRole"]
    }
}

resource "aws_iam_policy" "cinematica_api_lambda_logging_access" {
    name = "cinematica-api-lambda-logging-access"
    path = "/"
    description = "IAM policy that allows the Cinematica API lambda function to its corresponding log group"
    policy = data.aws_iam_policy_document.cinematica_api_lambda_log_policy_document.json
}

data "aws_iam_policy_document" "cinematica_api_lambda_log_policy_document" {
    statement {
        effect = "Allow"

        actions = [
            "logs:CreateLogGroup",
            "logs:CreateLogStream",
            "logs:PutLogEvents",
            "logs:GetLogEvents"
        ]

        resources = [aws_cloudwatch_log_group.cinematica_api_lambda_log_group.arn]
    }
}

resource "aws_iam_role_policy_attachment" "cinematica_api_lambda_log_policy_attachment" {
    role = aws_iam_role.cinematica_api_lambda_role.name
    policy_arn = aws_iam_policy.cinematica_api_lambda_logging_access.arn
}

resource "aws_cloudwatch_log_group" "cinematica_api_lambda_log_group" {
    name = "/aws/lambda/${var.cinematica_lambda_function_name}"
    retention_in_days = 14
}

# # # # # # # # #
#  API Gateway  #
# # # # # # # # #

resource "aws_api_gateway_rest_api" "cinematica_api_gateway" {
    name = "cinematica-api-gateway"
    provider = aws.us_east
    disable_execute_api_endpoint = true

    endpoint_configuration {
        types = ["EDGE"]
    }
}

resource "aws_api_gateway_domain_name" "cinematica_api_domain" {
    provider = aws.us_east
    domain_name = var.api_domain_name
    certificate_arn = aws_acm_certificate.api_certificate.arn
    security_policy = "TLS_1_2"
}

resource "aws_api_gateway_base_path_mapping" "api_mapping" {
    provider = aws.us_east
    api_id      = aws_api_gateway_rest_api.cinematica_api_gateway.id
    domain_name = aws_api_gateway_domain_name.cinematica_api_domain.domain_name
    stage_name = aws_api_gateway_stage.cinematica_production.stage_name
}

resource "aws_api_gateway_resource" "cinematica_api_gateway_resource" {
    provider = aws.us_east
    parent_id = aws_api_gateway_rest_api.cinematica_api_gateway.root_resource_id
    rest_api_id = aws_api_gateway_rest_api.cinematica_api_gateway.id
    path_part = "{proxy+}"
}

resource "aws_api_gateway_method" "cinematica_api_gateway_proxy_method" {
    provider = aws.us_east
    authorization = "NONE"
    http_method   = "ANY"
    resource_id   = aws_api_gateway_resource.cinematica_api_gateway_resource.id
    rest_api_id   = aws_api_gateway_rest_api.cinematica_api_gateway.id
}

resource "aws_api_gateway_integration" "cinematica_api_gateway_integration" {
    provider = aws.us_east
    http_method = aws_api_gateway_method.cinematica_api_gateway_proxy_method.http_method
    resource_id = aws_api_gateway_resource.cinematica_api_gateway_resource.id
    rest_api_id = aws_api_gateway_rest_api.cinematica_api_gateway.id
    type        = "AWS_PROXY"
    integration_http_method = "POST"
    uri = aws_lambda_function.cinematica_api_lambda.invoke_arn
}

resource "aws_lambda_permission" "api_gateway_lambda_permission" {
    statement_id  = "AllowExecutionFromAPIGateway"
    action        = "lambda:InvokeFunction"
    function_name = aws_lambda_function.cinematica_api_lambda.function_name
    principal     = "apigateway.amazonaws.com"
    source_arn = "${aws_api_gateway_rest_api.cinematica_api_gateway.execution_arn}/*"
}

resource "aws_api_gateway_deployment" "cinematica_deployment" {
    provider = aws.us_east
    rest_api_id = aws_api_gateway_rest_api.cinematica_api_gateway.id

    triggers = {
        redeployment = sha1(jsonencode([
            aws_api_gateway_resource.cinematica_api_gateway_resource.id,
            aws_api_gateway_method.cinematica_api_gateway_proxy_method.id,
            aws_api_gateway_integration.cinematica_api_gateway_integration.id
        ]))
    }

    lifecycle {
        create_before_destroy = true
    }
}

variable "stage_name" {
    default = "production"
}

resource "aws_api_gateway_stage" "cinematica_production" {
    provider = aws.us_east
    deployment_id = aws_api_gateway_deployment.cinematica_deployment.id
    rest_api_id = aws_api_gateway_rest_api.cinematica_api_gateway.id
    stage_name = var.stage_name

    depends_on = [aws_cloudwatch_log_group.cinematica_api_gateway_log_group]
}

resource "aws_api_gateway_account" "account" {
    provider = aws.us_east
    cloudwatch_role_arn = aws_iam_role.api_gateway_logging_role.arn
}

resource "aws_iam_role" "api_gateway_logging_role" {
    name = "APIGatewayCloudWatchRole"
    assume_role_policy = data.aws_iam_policy_document.api_gateway_assume_role.json
}

data "aws_iam_policy_document" "api_gateway_assume_role" {
    statement {
        effect = "Allow"

        principals {
            type = "Service"
            identifiers = ["apigateway.amazonaws.com"]
        }

        actions = ["sts:AssumeRole"]
    }
}

resource "aws_iam_policy" "api_gateway_logging_access" {
    name = "APIGatewayCloudWatchLogsAccess"
    path = "/"
    description = "IAM policy that allows API Gateway to write logs"
    policy = data.aws_iam_policy_document.api_gateway_log_policy_document.json
}

data "aws_iam_policy_document" "api_gateway_log_policy_document" {
    statement {
        effect = "Allow"

        actions = [
            "logs:CreateLogGroup",
            "logs:CreateLogStream",
            "logs:DescribeLogGroups",
            "logs:DescribeLogStreams",
            "logs:PutLogEvents",
            "logs:GetLogEvents",
            "logs:FilterLogEvents"
        ]

        resources = ["*"]
    }
}

resource "aws_iam_role_policy_attachment" "cinematica_api_gateway_log_policy_attachment" {
    role = aws_iam_role.api_gateway_logging_role.name
    policy_arn = aws_iam_policy.api_gateway_logging_access.arn
}

resource "aws_cloudwatch_log_group" "cinematica_api_gateway_log_group" {
    name = "API-Gateway-Execution-Logs_${aws_api_gateway_rest_api.cinematica_api_gateway.id}/${var.stage_name}"
    retention_in_days = 14
}

# # # # #
#  ACM  #
# # # # #

resource "aws_acm_certificate" "api_certificate" {
    domain_name = var.api_domain_name
    validation_method = "DNS"
    provider = aws.us_east

    lifecycle {
        create_before_destroy = true
    }
}

# # # # # # # #
#  CloudFlare #
# # # # # # # #

resource "cloudflare_record" "api_cinematica_social" {
    name    = var.api_domain_name
    type    = "CNAME"
    value   = aws_api_gateway_domain_name.cinematica_api_domain.cloudfront_domain_name
    zone_id = var.zone_id
    proxied = false
}

resource "cloudflare_record" "validation" {
    for_each = {
        for dvo in aws_acm_certificate.api_certificate.domain_validation_options : dvo.domain_name => {
            name   = dvo.resource_record_name
            record = dvo.resource_record_value
            type   = dvo.resource_record_type
        }
    }

    name    = trimsuffix(each.value.name, ".")
    type    = each.value.type
    value   = trimsuffix(each.value.record, ".")
    zone_id = var.zone_id
    proxied = false
}