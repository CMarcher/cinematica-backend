terraform {
    required_providers {
        aws = {
            source = "hashicorp/aws"
            version = "5.13.1"
        }

        cloudflare = {
            source = "cloudflare/cloudflare"
            version = "4.14.0"
        }
    }

    cloud {
        organization = "cinematica"

        workspaces {
            name = "cinematica-backend"
        }
    }
}

provider "aws" {
    region = "ap-southeast-2"
}

provider "aws" {
    alias = "us_east"
    region = "us-east-1"
}

provider "cloudflare" { }