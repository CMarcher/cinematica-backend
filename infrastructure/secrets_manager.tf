resource "aws_secretsmanager_secret" "tmdb_api_secret" {
    name = "TMDbApiKey"
    recovery_window_in_days = 0
}

resource "aws_secretsmanager_secret" "database_password" {
    name = "DB_PASSWORD"
    recovery_window_in_days = 0
}