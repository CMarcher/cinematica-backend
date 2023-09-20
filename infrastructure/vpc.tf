# Front-end VPC #

resource "aws_vpc" "web_app_vpc" {
    cidr_block = "10.0.0.0/16"
    
    tags = {
        Name = "Front-end"
    }
}

resource "aws_security_group" "web_app_security_group" {
    name = "web-app-ecs-security-group"
    vpc_id = aws_vpc.web_app_vpc.id
    
    ingress {
        protocol = "tcp"
        from_port = 80
        to_port = 3000
        security_groups = [aws_security_group.web_app_lb_security_group.id]
    }

    egress {
        protocol = "-1"
        from_port = 0
        to_port = 0
        cidr_blocks = ["0.0.0.0/0"]
    }
}

resource "aws_security_group" "web_app_lb_security_group" {
    name = "web-app-load-balancer-security-group"
    vpc_id = aws_vpc.web_app_vpc.id

    ingress {
        protocol = "tcp"
        from_port = 80
        to_port = 80
        cidr_blocks = ["0.0.0.0/0"]
    }
    
    ingress {
        protocol = "tcp"
        from_port = 443
        to_port = 443
        cidr_blocks = ["0.0.0.0/0"]
    }

    egress {
        protocol = "-1"
        from_port = 0
        to_port = 0
        cidr_blocks = ["0.0.0.0/0"]
    }
}

resource "aws_subnet" "web_app_public_subnet" {
    count = 2
    cidr_block = cidrsubnet(aws_vpc.web_app_vpc.cidr_block, 8, 2 + count.index)
    availability_zone = data.aws_availability_zones.available_zones.names[count.index]
    vpc_id = aws_vpc.web_app_vpc.id
    map_public_ip_on_launch = true
}

resource "aws_subnet" "web_app_private_subnet" {
    count = 2
    cidr_block = cidrsubnet(aws_vpc.web_app_vpc.cidr_block, 8, count.index)
    availability_zone = data.aws_availability_zones.available_zones.names[count.index]
    vpc_id = aws_vpc.web_app_vpc.id
}

resource "aws_internet_gateway" "web_app_gateway" {
    vpc_id = aws_vpc.web_app_vpc.id
}

resource "aws_route" "internet_access" {
    route_table_id = aws_vpc.web_app_vpc.main_route_table_id
    destination_cidr_block = "0.0.0.0/0"
    gateway_id = aws_internet_gateway.web_app_gateway.id
}

resource "aws_eip" "web_app_gateway" {
    count = 2
    domain = "vpc"
    depends_on = [aws_internet_gateway.web_app_gateway]
}

resource "aws_nat_gateway" "web_app_gateway" {
    count = 2
    subnet_id = element(aws_subnet.web_app_public_subnet.*.id, count.index)
    allocation_id = element(aws_eip.web_app_gateway.*.id, count.index)
}

resource "aws_route_table" "web_app_private" {
    count  = 2
    vpc_id = aws_vpc.web_app_vpc.id

    route {
        cidr_block = "0.0.0.0/0"
        nat_gateway_id = element(aws_nat_gateway.web_app_gateway.*.id, count.index)
    }
}

resource "aws_route_table_association" "web_app_private" {
    count          = 2
    subnet_id      = element(aws_subnet.web_app_private_subnet.*.id, count.index)
    route_table_id = element(aws_route_table.web_app_private.*.id, count.index)
}

# Back-end VPC #

resource "aws_vpc" "back_end_vpc" {
    cidr_block = "10.0.0.0/16"
    enable_dns_hostnames = true

    tags = {
        Name = "Back-end"
    }
}

resource "aws_security_group" "database_to_lambda_security_group" {
    name = "rds-lambda-1"
    vpc_id = aws_vpc.back_end_vpc.id
}

resource "aws_security_group_rule" "database_to_lambda_ingress_sg_rule" {
    from_port         = 0
    to_port           = 5432
    protocol          = "tcp"
    source_security_group_id = aws_security_group.api_to_rds_security_group.id
    security_group_id = aws_security_group.database_to_lambda_security_group.id
    type              = "ingress"
}

resource "aws_security_group_rule" "database_to_lambda_egress_sg_rule" {
    from_port         = 0
    to_port           = 0
    protocol          = "-1"
    cidr_blocks       = ["0.0.0.0/0"]
    security_group_id = aws_security_group.database_to_lambda_security_group.id
    type              = "egress"
}

resource "aws_security_group" "api_to_rds_security_group" {
    name = "lambda-rds-1"
    vpc_id = aws_vpc.back_end_vpc.id
}

resource "aws_security_group_rule" "api_to_rds_egress_sg_rule" {
    from_port         = 0
    to_port           = 5432
    protocol          = "tcp"
    source_security_group_id = aws_security_group.database_to_lambda_security_group.id
    security_group_id = aws_security_group.api_to_rds_security_group.id
    type              = "egress"
}

resource "aws_security_group" "api_internet_access_security_group" {
    name = "api-outbound-internet-security-group"

    egress {
        protocol = "-1"
        from_port = 0
        to_port = 0
        cidr_blocks = ["0.0.0.0/0"]
    }
}

resource "aws_subnet" "backend_public_subnet" {
    cidr_block = cidrsubnet(aws_vpc.back_end_vpc.cidr_block, 8, 1)
    availability_zone = data.aws_availability_zones.available_zones.names[0]
    vpc_id = aws_vpc.back_end_vpc.id
    map_public_ip_on_launch = true
}

resource "aws_subnet" "backend_api_private_subnet" {
    cidr_block = cidrsubnet(aws_vpc.back_end_vpc.cidr_block, 8, 2)
    availability_zone = data.aws_availability_zones.available_zones.names[0]
    vpc_id = aws_vpc.back_end_vpc.id
}

resource "aws_subnet" "backend_db_private_subnets" {
    count = 2
    cidr_block = cidrsubnet(aws_vpc.back_end_vpc.cidr_block, 8, 3 + count.index)
    availability_zone = data.aws_availability_zones.available_zones.names[count.index]
    vpc_id = aws_vpc.back_end_vpc.id
}

resource "aws_internet_gateway" "backend_gateway" {
    vpc_id = aws_vpc.back_end_vpc.id
}

resource "aws_route" "backend_internet_access_route" {
    route_table_id = aws_vpc.back_end_vpc.main_route_table_id
    destination_cidr_block = "0.0.0.0/0"
    gateway_id = aws_internet_gateway.backend_gateway.id
}

resource "aws_eip" "backend_gateway_address" {
    domain = "vpc"
    depends_on = [aws_internet_gateway.backend_gateway]
}

resource "aws_nat_gateway" "backend_nat_gateway" {
    subnet_id = aws_subnet.backend_public_subnet.id
    allocation_id = aws_eip.backend_gateway_address.id
}

resource "aws_route_table" "backend_private" {
    vpc_id = aws_vpc.back_end_vpc.id

    route {
        cidr_block = "0.0.0.0/0"
        nat_gateway_id = aws_nat_gateway.backend_nat_gateway.id
    }
}

resource "aws_route_table_association" "backend_api_private" {
    subnet_id      = aws_subnet.backend_api_private_subnet.id
    route_table_id = aws_route_table.backend_private.id
}

resource "aws_route_table_association" "backend_db_private" {
    count = 2
    subnet_id      = element(aws_subnet.backend_db_private_subnets.*.id, count.index)
    route_table_id = aws_route_table.backend_private.id
}