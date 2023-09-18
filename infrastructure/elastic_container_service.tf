resource "aws_ecs_cluster" "web_app_cluster" {
    name = "web-app-cluster"
    
}

resource "aws_ecs_service" "web_app_service" {
    name = "web-app-service"
    cluster = aws_ecs_cluster.web_app_cluster.id
    task_definition = aws_ecs_task_definition.web_app_task.arn
    desired_count = 1
    launch_type = "FARGATE"
    
    network_configuration {
        subnets = aws_subnet.web_app_private_subnet.*.id
        security_groups = [aws_security_group.web_app_security_group.id]
    }
    
    load_balancer {
        container_name = "front-end"
        container_port = 3000
        target_group_arn = aws_lb_target_group.web_app_lb_http_target_group.arn
    }
    
    lifecycle {
        ignore_changes = [desired_count]
    }
    
    depends_on = [
        aws_lb_listener.web_app_lb_http_redirect_listener,
        aws_lb_listener.web_app_lb_https_listener
    ]
}

resource "aws_ecs_task_definition" "web_app_task" {
    family = "cinematica-front-end"
    cpu = 512
    memory = 1024
    requires_compatibilities = ["FARGATE"]
    network_mode = "awsvpc"
    
    container_definitions = jsonencode([{
        name = "front-end"
        image = "${aws_ecrpublic_repository.web_app_image_repository.repository_uri}:latest"
        essential = true
        appProtocol = "HTTP"
        
        portMappings = [
            {
                containerPort = 3000
            }
        ]
        logConfiguration = {
            logDriver = "awslogs"
            options = {
                awslogs-group = aws_cloudwatch_log_group.web_app_tasks_log_group.name
                awslogs-region = "ap-south-east-2"
                awslogs-stream-prefix = "ecs"
            }
        }
    }])
    
    execution_role_arn = aws_iam_role.ecs_web_app_execution_role.arn
}

resource "aws_cloudwatch_log_group" "web_app_tasks_log_group" {
    name = "/aws/ecs/webapp"
    retention_in_days = 14
}

resource "aws_iam_role" "ecs_web_app_execution_role" {
    name = "ECSWebAppTaskExecutionRole"
    assume_role_policy = data.aws_iam_policy_document.ecs_task_trust_policy.json
}

data "aws_iam_policy_document" "ecs_task_trust_policy" {
    statement {
        effect = "Allow"
        
        principals {
            type = "Service"
            identifiers = ["ecs-tasks.amazonaws.com"]
        }
        
        actions = ["sts:AssumeRole"]
    }
}

resource "aws_iam_role_policy_attachment" "ecs_web_app_attachment" {
    policy_arn = aws_iam_policy.ecs_web_app_policy.arn
    role       = aws_iam_role.ecs_web_app_execution_role.name
}

resource "aws_iam_policy" "ecs_web_app_policy" {
    name = "ECSWebAppTaskPermissions"
    policy = data.aws_iam_policy_document.ecs_web_app_policy_document.json
}

data "aws_iam_policy_document" "ecs_web_app_policy_document" {
    statement {
        effect = "Allow"
        actions = ["logs:CreateLogStream", "logs:PutLogEvents"]
        resources = ["${aws_cloudwatch_log_group.web_app_tasks_log_group.arn}:*"]
    }
}