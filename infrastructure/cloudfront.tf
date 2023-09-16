resource "aws_cloudfront_distribution" "s3_distribution" {
  enabled = true

  origin {
    domain_name = aws_s3_bucket.media_bucket.bucket_regional_domain_name
    origin_id   = local.s3_origin_id

    s3_origin_config {
      origin_access_identity = aws_cloudfront_origin_access_identity.cloudfront_s3_identity.cloudfront_access_identity_path
    }
  }

  aliases = [var.cdn_domain_name]

  viewer_certificate {
    acm_certificate_arn = aws_acm_certificate.cdn_certificate.arn
    minimum_protocol_version = "TLSv1.2_2018"
    ssl_support_method = "sni-only"
    cloudfront_default_certificate = false
  }

  default_cache_behavior {
    target_origin_id       = local.s3_origin_id
    viewer_protocol_policy = "redirect-to-https"
    allowed_methods = ["HEAD", "GET"]
    cached_methods = ["HEAD", "GET"]

    forwarded_values {
      query_string = false

      cookies {
        forward = "none"
      }
    }

    compress = true
  }

  restrictions {
    geo_restriction {
      locations = []
      restriction_type = "none"
    }
  }
}

resource "aws_cloudfront_origin_access_identity" "cloudfront_s3_identity" { }