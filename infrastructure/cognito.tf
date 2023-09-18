resource "aws_cognito_user_pool_client" "cinematica_cognito_client" {
    access_token_validity                         = 60
    allowed_oauth_flows                           = ["implicit"]
    allowed_oauth_flows_user_pool_client          = true
    allowed_oauth_scopes                          = ["dotnet/dotnet"]
    auth_session_validity                         = 3
    callback_urls                                 = ["https://cinematica.social"]
    enable_propagate_additional_user_context_data = false
    enable_token_revocation                       = true
    explicit_auth_flows                           = ["ALLOW_REFRESH_TOKEN_AUTH", "ALLOW_USER_PASSWORD_AUTH", "ALLOW_USER_SRP_AUTH"]
    id_token_validity                             = 60
    name                                          = "cinematica-app-client"
    prevent_user_existence_errors                 = "ENABLED"
    read_attributes                               = ["address", "birthdate", "email", "email_verified", "family_name", "gender", "given_name", "locale", "middle_name", "name", "nickname", "phone_number", "phone_number_verified", "picture", "preferred_username", "profile", "updated_at", "website", "zoneinfo"]
    refresh_token_validity                        = 30
    supported_identity_providers                  = ["COGNITO"]
    user_pool_id                                  = "ap-southeast-2_FGMQ93JVg"
    write_attributes                              = ["address", "birthdate", "email", "family_name", "gender", "given_name", "locale", "middle_name", "name", "nickname", "phone_number", "picture", "preferred_username", "profile", "updated_at", "website", "zoneinfo"]
    
    token_validity_units {
        access_token  = "minutes"
        id_token      = "minutes"
        refresh_token = "days"
    }
}

resource "aws_cognito_user_pool" "cinematica_user_pool" {
    alias_attributes           = ["email"]
    auto_verified_attributes   = ["email"]
    deletion_protection        = "ACTIVE"
    mfa_configuration          = "OFF"
    name                       = "cinematica"

    account_recovery_setting {
        recovery_mechanism {
            name     = "verified_email"
            priority = 1
        }
    }

    admin_create_user_config {
        allow_admin_create_user_only = false
    }

    email_configuration {
        email_sending_account  = "COGNITO_DEFAULT"
    }
    
    password_policy {
        minimum_length                   = 8
        require_lowercase                = true
        require_numbers                  = true
        require_symbols                  = true
        require_uppercase                = true
        temporary_password_validity_days = 7
    }
    
    schema {
        attribute_data_type      = "String"
        developer_only_attribute = false
        mutable                  = true
        name                     = "email"
        required                 = true
        
        string_attribute_constraints {
            max_length = "2048"
            min_length = "0"
        }
    }
    
    user_attribute_update_settings {
        attributes_require_verification_before_update = ["email"]
    }
    
    username_configuration {
        case_sensitive = false
    }
    
    verification_message_template {
        default_email_option  = "CONFIRM_WITH_CODE"
    }
}
