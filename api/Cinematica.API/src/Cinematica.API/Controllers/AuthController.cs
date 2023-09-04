using Microsoft.AspNetCore.Mvc;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Cinematica.API.Models.Cognito;
using Amazon.Extensions.CognitoAuthentication;
using Amazon;
using System.Net;

namespace Cinematica.API.Controllers;

[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration APP_CONFIG;
    private AmazonCognitoIdentityProviderClient cognitoIdClient;

    public AuthController(IConfiguration config) {
        APP_CONFIG = config.GetSection("AWS");

        cognitoIdClient = new AmazonCognitoIdentityProviderClient
        (
            APP_CONFIG.GetValue<string>("AccessKeyId"), 
            APP_CONFIG.GetValue<string>("AccessSecretKey"), 
            RegionEndpoint.GetBySystemName(APP_CONFIG.GetValue<string>("Region"))
        );
    }
    
    // POST api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest model)
    {
        try {      
            var user = await FindUserByEmailAddress(model.Email);
            if(user == null) {
                var regRequest = new SignUpRequest
                {
                    ClientId = APP_CONFIG.GetValue<string>("AppClientId"),
                    Username = model.Username,
                    Password = model.Password,
                    UserAttributes = { new AttributeType { Name = "email", Value = model.Email } }
                };
                var ret = await cognitoIdClient.SignUpAsync(regRequest);
                return Ok(new { message = "Registration successful." });;
            }
            else {
                return BadRequest(new { message = "Email already registered." });
            } 
        }
        catch(Exception e) {
            return BadRequest(new { message = e.ToString().Split("\r\n")[0] });
        }
    }

    // POST api/auth/login
    [HttpPost("login")]    
    public async Task<IActionResult> Login(LoginRequest model)
    {
        try
        {
            var cognitoUserPool = new CognitoUserPool(APP_CONFIG.GetValue<string>("UserPoolId"), APP_CONFIG.GetValue<string>("AppClientId"), cognitoIdClient);
            var cognitoUser = new CognitoUser(model.Username, APP_CONFIG.GetValue<string>("AppClientId"), cognitoUserPool, cognitoIdClient);
        
            InitiateSrpAuthRequest authRequest = new InitiateSrpAuthRequest()
            {
                Password = model.Password
            };

            AuthFlowResponse authResponse = await cognitoUser.StartWithSrpAuthAsync(authRequest);
            var result = authResponse.AuthenticationResult;

            return Ok(new { idToken = result.IdToken, accessToken = result.AccessToken, refreshToken = result.RefreshToken });
        }
        catch (UserNotConfirmedException)
        {
            return BadRequest(new { message = "User hasn't been verified yet." });
        }
        catch (UserNotFoundException)
        {
            return BadRequest(new { message = "Username not found." });
        }
        catch (NotAuthorizedException)
        {
            return BadRequest(new { message = "Incorrect username or password." });
        }
    }

    // POST api/auth/confirm-registration
    [HttpPost("confirm-registration")]
    public async Task<IActionResult> ConfirmRegistration(ConfirmRegistrationRequest model)
    {
        try {
            var regRequest = new ConfirmSignUpRequest
                {
                    ClientId = APP_CONFIG.GetValue<string>("AppClientId"),
                    Username = model.Username,
                    ConfirmationCode = model.ConfirmationCode
                };

            var ret = await cognitoIdClient.ConfirmSignUpAsync(regRequest);
            return Ok(new { message = "Verification successful." });
        }
        catch(Exception e) {
            return BadRequest(new { message = e.ToString().Split("\r\n")[0] });
        }
    }

    // POST api/auth/resend-confirmation-code
    [HttpPost("resend-confirmation-code")]
    public async Task<IActionResult> ResendConfirmationCode([FromForm] string email)
    {
        try {
            var user = await FindUserByEmailAddress(email);

            if (user != null) {
                var forgotPasswordResponse = await cognitoIdClient.ResendConfirmationCodeAsync
                (
                    new ResendConfirmationCodeRequest 
                    { 
                        ClientId = APP_CONFIG.GetValue<string>("AppClientId"),
                        Username = user.Username
                    }
                ); 
                
                return Ok(new { message = "Confirmation code has been sent." });
        }
        else 
        {
            return BadRequest(new { message = "Email not found." });
        }
        }
        catch(Exception e) {
            return BadRequest(new { message = e.ToString().Split("\r\n")[0] });
        }
    }

    // POST api/auth/request-password-reset
    [HttpPost("request-password-reset")]    
    public async Task<IActionResult> RequestPasswordReset([FromForm] string email)
    {
        try {
            var user = await FindUserByEmailAddress(email);

            if (user != null) {
                var forgotPasswordResponse = await cognitoIdClient.ForgotPasswordAsync
                (
                    new ForgotPasswordRequest 
                    { 
                        ClientId = APP_CONFIG.GetValue<string>("AppClientId"),
                        Username = user.Username
                    }
                ); 
                
                return Ok(new { message = "Reset password email has been sent." });
        }
        else 
        {
            return BadRequest(new { message = "Email not found." });
        }
        }
        catch(Exception e) {
            return BadRequest(new { message = e.ToString().Split("\r\n")[0] });
        }
    }

    // POST api/auth/reset-password
    [HttpPost("reset-password")] 
    public async Task<IActionResult> ResetPassword(ResetPassword model)
    {
        try
        {
            var user = await FindUserByEmailAddress(model.Email);
            if(user != null) {
                var response = await cognitoIdClient.ConfirmForgotPasswordAsync(new ConfirmForgotPasswordRequest
                {
                    ClientId = APP_CONFIG.GetValue<string>("AppClientId"),
                    Username = user.Username,
                    Password = model.Password,
                    ConfirmationCode = model.ConfirmationCode
                });
                return Ok(new { message = "Password has been reset." });
            }
            else {
                return BadRequest(new { message = "Email not found." });
            }
            
        } 
        catch (Exception e)
        { 
            return BadRequest(new { message = e.ToString().Split("\r\n")[0] });
        } 
    }

    // Helper function to find a user by email address (assuming that email is unique)
    private async Task<UserType?> FindUserByEmailAddress(string emailAddress)
    {
        ListUsersRequest listUsersRequest = new ListUsersRequest
        {
            UserPoolId = APP_CONFIG.GetValue<string>("UserPoolId"),
            Filter = "email = \"" + emailAddress + "\""
        }; 
        
        var listUsersResponse = await cognitoIdClient.ListUsersAsync(listUsersRequest);

        if (listUsersResponse.HttpStatusCode == HttpStatusCode.OK) 
        {
            var users = listUsersResponse.Users; 
            return users.FirstOrDefault();
        }
        else 
        {
            return null;
        }
    } 
}