resource "aws_vpc" "web_app_vpc" {
    cidr_block = "10.0.0.0/16"
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