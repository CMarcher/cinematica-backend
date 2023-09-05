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

resource "aws_s3_bucket" "api_lambda_bucket" {
    bucket = "cinematica-api-lambda-source"
}