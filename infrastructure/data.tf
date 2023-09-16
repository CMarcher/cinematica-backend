data "aws_region" "api_gateway_region" { provider = aws.us_east }
data "aws_caller_identity" "current" {}
data "cloudflare_zone" "cinematica_social" { name = "cinematica.social" }