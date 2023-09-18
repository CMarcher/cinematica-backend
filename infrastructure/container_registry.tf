resource "aws_ecrpublic_repository" "web_app_image_repository" {
    provider = aws.us_east
    repository_name = "cinematica"
}

resource "aws_ecrpublic_repository_policy" "repository_lifecycle" {
    repository_name = aws_ecrpublic_repository.web_app_image_repository.repository_name
    
    policy = jsonencode({
        rules = [{
            rulePriority = 1
            description = "Delete older images"
            selection = {
                tagStatus = "any"
                countType = "imageCountMoreThan"
                countNumber = 2
            }
            action = {
                type = "expire"
            }
        }]
    })
}