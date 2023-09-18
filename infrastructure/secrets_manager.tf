resource "aws_secretsmanager_secret" "tmdb_api_secret" {
    name = "TMDbApiKey"
}

resource "aws_secretsmanager_secret" "database_password" {
    name = "DB_PASSWORD"
}