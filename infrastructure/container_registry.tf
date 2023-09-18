resource "aws_ecrpublic_repository" "web_app_image_repository" {
    provider = aws.us_east
    repository_name = "cinematica"
}