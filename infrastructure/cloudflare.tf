resource "cloudflare_record" "api_cinematica_social" {
  name    = var.api_domain_name
  type    = "CNAME"
  value   = aws_api_gateway_domain_name.cinematica_api_domain.cloudfront_domain_name
  zone_id = data.cloudflare_zone.cinematica_social.zone_id
  proxied = false
  allow_overwrite = true
}

resource "cloudflare_record" "api_validation" {
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
  zone_id = data.cloudflare_zone.cinematica_social.zone_id
  proxied = false
  allow_overwrite = true
}

resource "cloudflare_record" "cdn_cinematica_social" {
  name    = var.cdn_domain_name
  type    = "CNAME"
  value   = aws_cloudfront_distribution.s3_distribution.domain_name
  zone_id = data.cloudflare_zone.cinematica_social.zone_id
  proxied = false
  allow_overwrite = true
}

resource "cloudflare_record" "cdn_validation" {
  for_each = {
    for dvo in aws_acm_certificate.cdn_certificate.domain_validation_options : dvo.domain_name => {
      name   = dvo.resource_record_name
      record = dvo.resource_record_value
      type   = dvo.resource_record_type
    }
  }

  name    = trimsuffix(each.value.name, ".")
  type    = each.value.type
  value   = trimsuffix(each.value.record, ".")
  zone_id = data.cloudflare_zone.cinematica_social.zone_id
  proxied = false
  allow_overwrite = true
}

resource "cloudflare_record" "cinematica_social" {
  name    = var.cdn_domain_name
  type    = "A"
  value   = aws_lb.web_app_load_balancer.dns_name
  zone_id = data.cloudflare_zone.cinematica_social.zone_id
  proxied = false
  allow_overwrite = true
}

resource "cloudflare_record" "root_validation" {
  for_each = {
    for dvo in aws_acm_certificate.root_domain_certificate.domain_validation_options : dvo.domain_name => {
      name   = dvo.resource_record_name
      record = dvo.resource_record_value
      type   = dvo.resource_record_type
    }
  }

  name    = trimsuffix(each.value.name, ".")
  type    = each.value.type
  value   = trimsuffix(each.value.record, ".")
  zone_id = data.cloudflare_zone.cinematica_social.zone_id
  proxied = false
  allow_overwrite = true
}