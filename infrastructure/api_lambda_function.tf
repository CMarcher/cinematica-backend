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

    environment {
        variables = {
            DOTNET_DB_HOST = ""
            DOTNET_DB_USERNAME = ""
            DOTNET_DB_DATABASE = ""
            ASPNETCORE_ENVIRONMENT = "Production"
            Region = "ap-southeast-2",
            UserPoolId = aws_cognito_user_pool.cinematica_user_pool.id,
            AppClientId = aws_cognito_user_pool_client.cinematica_cognito_client.id,
            Authority = "https://cognito-idp.ap-southeast-2.amazonaws.com/${aws_cognito_user_pool.cinematica_user_pool.id}"
        }
    }

    depends_on = [
        aws_iam_role_policy_attachment.cinematica_api_lambda_log_policy_attachment,
        aws_cloudwatch_log_group.cinematica_api_lambda_log_group
    ]
}

resource "aws_iam_role" "cinematica_api_lambda_role" {
    name = "CinematicaAPILambdaExecutionRole"
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

resource "aws_iam_policy" "cinematica_api_lambda_permissions" {
    name = "CinematicaAPILambdaPermissionsPolicy"
    path = "/"
    description = "IAM policy that allows the Cinematica API lambda function to its corresponding log group"
    policy = data.aws_iam_policy_document.cinematica_api_lambda_policy_document.json
}

data "aws_iam_policy_document" "cinematica_api_lambda_policy_document" {
    statement {
        effect = "Allow"

        actions = [
            "logs:CreateLogGroup",
            "logs:CreateLogStream",
            "logs:PutLogEvents",
            "logs:GetLogEvents"
        ]

        resources = ["${aws_cloudwatch_log_group.cinematica_api_lambda_log_group.arn}:*"]
    }
    
    statement {
        effect = "Allow"
        
        actions = [
            "secretsmanager:GetSecretValue"
        ]
        
        resources = [
            aws_secretsmanager_secret.database_password.arn,
            aws_secretsmanager_secret.tmdb_api_secret.arn
        ]
    }
}

resource "aws_iam_role_policy_attachment" "cinematica_api_lambda_log_policy_attachment" {
    role = aws_iam_role.cinematica_api_lambda_role.name
    policy_arn = aws_iam_policy.cinematica_api_lambda_permissions.arn
}

resource "aws_cloudwatch_log_group" "cinematica_api_lambda_log_group" {
    name = "/aws/lambda/${var.cinematica_lambda_function_name}"
    retention_in_days = 14
}