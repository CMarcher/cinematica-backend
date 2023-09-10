using Microsoft.AspNetCore.Mvc;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Cinematica.API.Models.Cognito;
using Amazon.Extensions.CognitoAuthentication;
using Amazon;
using System.Net;
using Cinematica.API.Data;
using Cinematica.API.Models.Database;

namespace Cinematica.API.Controllers;

[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration APP_CONFIG;
    private DataContext context;
    
    private AmazonCognitoIdentityProviderClient cognitoIdClient;

    public AuthController(IConfiguration config, DataContext context) {
        APP_CONFIG = config.GetSection("AWS");
        this.context = context;

        cognitoIdClient = new AmazonCognitoIdentityProviderClient
        (
            APP_CONFIG["AccessKeyId"], 
            APP_CONFIG["AccessSecretKey"], 
            RegionEndpoint.GetBySystemName(APP_CONFIG["Region"])
        );
    }

    // POST api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest model)
    {
        try {      
            var user = await FindUserByEmailAddress(model.Email);
            if(user == null) {
                // create and send registration request to cognito
                var regRequest = new SignUpRequest
                {
                    ClientId = APP_CONFIG["AppClientId"],
                    Username = model.Username,
                    Password = model.Password,
                    UserAttributes = { new AttributeType { Name = "email", Value = model.Email } }
                };
                var ret = await cognitoIdClient.SignUpAsync(regRequest);



                return Ok(new { message = "Registration successful." });
            }
            else {
                return BadRequest(new { message = "Email already registered." });
            } 
        }
        catch(UsernameExistsException) {
            return BadRequest(new { message = "Username already registered" });
        }
        catch(InvalidPasswordException) {
            return BadRequest(new { message = "Invalid password." });
        }
        catch(Exception e) {
            return BadRequest(new { message = e.GetType().ToString() });
        }
    }

    // POST api/auth/login
    [HttpPost("login")]    
    public async Task<IActionResult> Login([FromBody] LoginRequest model)
    {
        try
        {
            var cognitoUserPool = new CognitoUserPool(APP_CONFIG["UserPoolId"], APP_CONFIG["AppClientId"], cognitoIdClient);
            var cognitoUser = new CognitoUser(model.Username, APP_CONFIG["AppClientId"], cognitoUserPool, cognitoIdClient);
        
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
        catch(Exception e) {
            return BadRequest(new { message = e.ToString().Split("\r\n")[0] });
        }
    }

    // POST api/auth/confirm-registration
    [HttpPost("confirm-registration")]
    public async Task<IActionResult> ConfirmRegistration([FromBody] ConfirmRegistrationRequest model)
    {
        try {
            var regRequest = new ConfirmSignUpRequest
                {
                    ClientId = APP_CONFIG["AppClientId"],
                    Username = model.Username,
                    ConfirmationCode = model.ConfirmationCode
                };

            var ret = await cognitoIdClient.ConfirmSignUpAsync(regRequest);

            // add user id to the database
            var getRequest = new AdminGetUserRequest()
            {
                UserPoolId = APP_CONFIG["UserPoolId"],
                Username = model.Username,
            };
            var newUser = await cognitoIdClient.AdminGetUserAsync(getRequest);
            context.Users.Add(new User { UserId = newUser.UserAttributes.ToArray()[0].Value, ProfilePicture = null, CoverPicture = null });
            context.SaveChanges();

            return Ok(new { message = "Verification successful." });
        }
        catch(CodeMismatchException) {
            return BadRequest(new { message = "Incorrect code." });
        }
        catch(ExpiredCodeException) {
            return BadRequest(new { message = "Expired code." });
        }
        catch(UserNotFoundException) {
            return BadRequest(new { message = "User doesn't exist." });
        }
        catch(Exception e) {
            return BadRequest(new { message = e.GetType().ToString() });
        }
    }

    // POST api/auth/resend-confirmation-code
    [HttpPost("resend-confirmation-code")]
    public async Task<IActionResult> ResendConfirmationCode([FromBody] string email)
    {
        try {
            var user = await FindUserByEmailAddress(email);

            if (user != null) {
                var forgotPasswordResponse = await cognitoIdClient.ResendConfirmationCodeAsync
                (
                    new ResendConfirmationCodeRequest 
                    { 
                        ClientId = APP_CONFIG["AppClientId"],
                        Username = user.Username
                    }
                ); 
                
                return Ok(new { message = "Confirmation code has been sent." });
            }
            else {
                return BadRequest(new { message = email + " Email not found." });
            }
        }
        catch(Exception e) {
            return BadRequest(new { message = e.GetType().ToString() });
        }
    }

    // POST api/auth/request-password-reset
    [HttpPost("request-password-reset")]    
    public async Task<IActionResult> RequestPasswordReset([FromBody] string email)
    {
        try {
            var user = await FindUserByEmailAddress(email);

            if (user != null) {
                var forgotPasswordResponse = await cognitoIdClient.ForgotPasswordAsync
                (
                    new ForgotPasswordRequest 
                    { 
                        ClientId = APP_CONFIG["AppClientId"],
                        Username = user.Username
                    }
                ); 
                
                return Ok(new { message = "Reset password email has been sent." });
            }
            else {
                return BadRequest(new { message = "Email not found." });
            }
        }
        catch(UserNotConfirmedException) {
            return BadRequest(new { message = "User hasn't been verified." });
        }
        catch(InvalidPasswordException) {
            return BadRequest(new { message = "Invalid password." });
        }
        catch(Exception e) {
            return BadRequest(new { message = e.GetType().ToString() });
        }
    }

    // POST api/auth/reset-password
    [HttpPost("reset-password")] 
    public async Task<IActionResult> ResetPassword([FromBody] ResetPassword model)
    {
        try
        {
            var user = await FindUserByEmailAddress(model.Email);
            if(user != null) {
                var response = await cognitoIdClient.ConfirmForgotPasswordAsync(new ConfirmForgotPasswordRequest
                {
                    ClientId = APP_CONFIG["AppClientId"],
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
        
        catch(Exception e) {
            return BadRequest(new { message = e.GetType().ToString() });
        }
    }

    // Helper function to find a user by email address (assuming that email is unique)
    private async Task<UserType?> FindUserByEmailAddress(string emailAddress)
    {
        ListUsersRequest listUsersRequest = new ListUsersRequest
        {
            UserPoolId = APP_CONFIG["UserPoolId"],
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


