resource "aws_acm_certificate" "api_certificate" {
  domain_name = var.api_domain_name
  validation_method = "DNS"
  provider = aws.us_east

  lifecycle {
    create_before_destroy = true
  }
}

resource "aws_acm_certificate" "cdn_certificate" {
  domain_name = var.cdn_domain_name
  validation_method = "DNS"
  provider = aws.us_east

  lifecycle {
    create_before_destroy = true
  }
}