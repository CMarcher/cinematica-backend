resource "aws_lb" "web_app_load_balancer" {
    name = "web-app-load-balancer"
    internal = false
    load_balancer_type = "application"
    security_groups = [aws_security_group.web_app_lb_security_group.id]
    subnets = [aws_subnet.web_app_public_subnet.*.id]
    ip_address_type = "ipv4"
    enable_deletion_protection = false
}

resource "aws_lb_listener" "web_app_lb_https_listener" {
    load_balancer_arn = aws_lb.web_app_load_balancer.arn
    protocol = "HTTPS"
    port = 443
    ssl_policy = "ELBSecurityPolicy-TLS13-1-2-2021-06"
    
    default_action {
        type = "forward"
        target_group_arn = aws_lb_target_group.web_app_lb_http_target_group.arn
    }
    
    certificate_arn = aws_acm_certificate.root_domain_certificate.arn
}

resource "aws_lb_listener" "web_app_lb_http_redirect_listener" {
    load_balancer_arn = aws_lb.web_app_load_balancer.arn
    port              = "80"
    protocol          = "HTTP"

    default_action {
        type = "redirect"

        redirect {
            port        = "443"
            protocol    = "HTTPS"
            status_code = "HTTP_301"
        }
    }
}

resource "aws_lb_target_group" "web_app_lb_http_target_group" {
    name = "web-app-target-group"
    target_type = "ip"
    ip_address_type = "ipv4"
    protocol = "HTTP"
    port = 3000
    vpc_id = aws_vpc.web_app_vpc.id
    
    health_check {
        enabled = true
        healthy_threshold = 2
        interval = 30
        matcher = 200
        path = "/"
        port = "traffic-port"
        timeout = 15
        unhealthy_threshold = 3
    }
}