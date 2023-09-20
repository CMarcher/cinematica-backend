resource "aws_db_instance" "cinematica_database" {
    identifier = "cinematica-postgres-database"
    instance_class = "db.t3.micro"
    engine = "postgres"
    engine_version = "15.3"
    allocated_storage = 20
    db_name = "cinematica"
    username = "cinematica-admin"
    manage_master_user_password = true
    port = 5432
    
    db_subnet_group_name = aws_db_subnet_group.database_subnet_group.id
    vpc_security_group_ids = [aws_security_group.database_to_lambda_security_group.id]
}

resource "aws_db_subnet_group" "database_subnet_group" {
    subnet_ids = aws_subnet.backend_db_private_subnets.*.id
}