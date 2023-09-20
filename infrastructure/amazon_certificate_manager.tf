resource "aws_acm_certificate" "api_certificate" {
    domain_name = var.api_domain_name
    validation_method = "DNS"
    provider = aws.us_east

    lifecycle {
        create_before_destroy = true
    }
}

resource "aws_acm_certificate_validation" "api_certificate_validation" {
    certificate_arn = aws_acm_certificate.api_certificate.arn
    validation_record_fqdns = [aws_acm_certificate.api_certificate.domain_name]
}

resource "aws_acm_certificate" "cdn_certificate" {
    domain_name = var.cdn_domain_name
    validation_method = "DNS"
    provider = aws.us_east

    lifecycle {
        create_before_destroy = true
    }
}

resource "aws_acm_certificate_validation" "cdn_certificate_validation" {
    certificate_arn = aws_acm_certificate.cdn_certificate.arn
    validation_record_fqdns = [aws_acm_certificate.cdn_certificate.domain_name]
}

resource "aws_acm_certificate" "root_domain_certificate" {
    domain_name = var.root_domain_name
    validation_method = "DNS"

    lifecycle {
        create_before_destroy = true
    }
}

resource "aws_acm_certificate_validation" "root_certificate_validation" {
    certificate_arn = aws_acm_certificate.root_domain_certificate.arn
    validation_record_fqdns = [aws_acm_certificate.root_domain_certificate.domain_name]
}