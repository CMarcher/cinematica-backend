terraform {
    required_providers {
        aws = {
            source = "hashicorp/aws"
            version = "5.13.1"
        }
    }
}

provider "aws" {
    region = "ap-southeast-2"
}

data "aws_caller_identity" "current" {}

locals {
    account_id = data.aws_caller_identity.current.account_id
}

variable "region" { default = "ap-southeast-2" }
variable "api_domain_name" { default = "api.cinematica.social" }

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

        resources = ["arn:aws:logs:ap-southeast-2:${local.account_id}:log-group:${aws_cloudwatch_log_group.cinematica_api_lambda_log_group.name}/*"]
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
}

resource "aws_api_gateway_domain_name" "cinematica_api_domain" {
    domain_name = var.api_domain_name
    certificate_arn = aws_acm_certificate.api_certificate.arn
}

resource "aws_api_gateway_resource" "cinematica_api_gateway_resource" {
    parent_id = aws_api_gateway_rest_api.cinematica_api_gateway.root_resource_id
    rest_api_id = aws_api_gateway_rest_api.cinematica_api_gateway.id
    path_part = "{proxy+}"
}

resource "aws_api_gateway_method" "cinematica_api_gateway_proxy_method" {
    authorization = "NONE"
    http_method   = "ANY"
    resource_id   = aws_api_gateway_resource.cinematica_api_gateway_resource.id
    rest_api_id   = aws_api_gateway_rest_api.cinematica_api_gateway.id
}

resource "aws_api_gateway_integration" "cinematica_api_gateway_integration" {
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
    source_arn = "arn:aws:execute-api:${var.region}:${local.account_id}:${aws_api_gateway_rest_api.cinematica_api_gateway.id}/*/${aws_api_gateway_method.cinematica_api_gateway_proxy_method.http_method}${aws_api_gateway_resource.cinematica_api_gateway_resource.path}"
}

# # # # #
#  ACM  #
# # # # #

resource "aws_acm_certificate" "api_certificate" {
    domain_name = var.api_domain_name
    validation_method = "DNS"

    lifecycle {
        create_before_destroy = true
    }
}

resource "aws_acm_certificate_validation" "validator" {
    certificate_arn = aws_acm_certificate.api_certificate.arn
}