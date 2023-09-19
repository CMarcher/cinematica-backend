using Cinematica.API.Data;
using Cinematica.API.Models.Database;
using Cinematica.API.Models.Cognito;
using Cinematica.API.Services;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Extensions.CognitoAuthentication;
using Amazon;
using Microsoft.AspNetCore.Mvc;
using System.Net;


namespace Cinematica.API.Controllers;

[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IHelperService _helper;
    private DataContext _context;
    private AmazonCognitoIdentityProviderClient _cognitoClient;

    public AuthController(IConfiguration config, IHelperService helperService, DataContext context, AmazonCognitoIdentityProviderClient client) {
        _config = config.GetSection("AWS");
        _context = context;
        _helper = helperService;
        _cognitoClient = client;
    }

    // POST api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest model)
    {
        try {      
            var user = await _helper.FindUserByEmailAddress(model.Email);
            if(user == null) {
                // create and send registration request to cognito
                var regRequest = new SignUpRequest
                {
                    ClientId = _config["AppClientId"],
                    Username = model.Username,
                    Password = model.Password,
                    UserAttributes = { new AttributeType { Name = "email", Value = model.Email } }
                };
                var ret = await _cognitoClient.SignUpAsync(regRequest);

                return Ok(new { message = "Registration successful." });
            }
            else {
                return BadRequest(new { message = "Email already registered." });
            } 
        }
        catch(Exception exception) {
            return BadRequest(ExceptionHander.HandleException(exception));
        }
    }

    // POST api/auth/login
    [HttpPost("login")]    
    public async Task<IActionResult> Login([FromBody] LoginRequest model)
    {
        try
        {
            var cognitoUserPool = new CognitoUserPool(_config["UserPoolId"], _config["AppClientId"], _cognitoClient);
            var cognitoUser = new CognitoUser(model.Username, _config["AppClientId"], cognitoUserPool, _cognitoClient);
        
            InitiateSrpAuthRequest authRequest = new InitiateSrpAuthRequest()
            {
                Password = model.Password
            };

            AuthFlowResponse authResponse = await cognitoUser.StartWithSrpAuthAsync(authRequest);
            var result = authResponse.AuthenticationResult;
            // add user id to the database

            var getRequest = new AdminGetUserRequest()
            {
                UserPoolId = _config["UserPoolId"],
                Username = model.Username,
            };
            var user = await _cognitoClient.AdminGetUserAsync(getRequest);

            return Ok(new { user_id = user.UserAttributes.ToArray()[0].Value, idToken = result.IdToken, refreshToken = result.RefreshToken });
        }
        catch(Exception exception) {
            return BadRequest(ExceptionHander.HandleException(exception));
        }
    }

    // POST api/auth/confirm-registration
    [HttpPost("confirm-registration")]
    public async Task<IActionResult> ConfirmRegistration([FromBody] ConfirmRegistrationRequest model)
    {
        try {
            var regRequest = new ConfirmSignUpRequest
                {
                    ClientId = _config["AppClientId"],
                    Username = model.Username,
                    ConfirmationCode = model.ConfirmationCode
                };

            var ret = await _cognitoClient.ConfirmSignUpAsync(regRequest);

            // add user id to the database
            var getRequest = new AdminGetUserRequest()
            {
                UserPoolId = _config["UserPoolId"],
                Username = model.Username,
            };
            var newUser = await _cognitoClient.AdminGetUserAsync(getRequest);
            _context.Add(new User { UserId = newUser.UserAttributes.ToArray()[0].Value, ProfilePicture = null, CoverPicture = null, UserName = model.Username});
            _context.SaveChanges();

            return Ok(new { message = "Verification successful." });
        }
        catch(Exception exception) {
            return BadRequest(ExceptionHander.HandleException(exception));
        }
    }

    // POST api/auth/resend-confirmation-code
    [HttpPost("resend-confirmation-code")]
    public async Task<IActionResult> ResendConfirmationCode([FromBody] string email)
    {
        try {
            var user = await _helper.FindUserByEmailAddress(email);

            if (user != null) {
                var forgotPasswordResponse = await _cognitoClient.ResendConfirmationCodeAsync
                (
                    new ResendConfirmationCodeRequest 
                    { 
                        ClientId = _config["AppClientId"],
                        Username = user.Username
                    }
                ); 
                
                return Ok(new { message = "Confirmation code has been sent." });
            }
            else {
                return BadRequest(new { message = email + " Email not found." });
            }
        }
        catch(Exception exception) {
            return BadRequest(ExceptionHander.HandleException(exception));
        }
    }

    // POST api/auth/request-password-reset
    [HttpPost("request-password-reset")]    
    public async Task<IActionResult> RequestPasswordReset([FromBody] string email)
    {
        try {
            var user = await _helper.FindUserByEmailAddress(email);

            if (user != null) {
                var forgotPasswordResponse = await _cognitoClient.ForgotPasswordAsync
                (
                    new ForgotPasswordRequest 
                    { 
                        ClientId = _config["AppClientId"],
                        Username = user.Username
                    }
                ); 
                
                return Ok(new { message = "Reset password email has been sent." });
            }
            else {
                return BadRequest(new { message = "Email not found." });
            }
        }
        catch(Exception exception) {
            return BadRequest(ExceptionHander.HandleException(exception));
        }
    }

    // POST api/auth/reset-password
    [HttpPost("reset-password")] 
    public async Task<IActionResult> ResetPassword([FromBody] ResetPassword model)
    {
        try
        {
            var user = await _helper.FindUserByEmailAddress(model.Email);
            if(user != null) {
                var response = await _cognitoClient.ConfirmForgotPasswordAsync(new ConfirmForgotPasswordRequest
                {
                    ClientId = _config["AppClientId"],
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
        
        catch(Exception exception) {
            return BadRequest(ExceptionHander.HandleException(exception));
        }
    }

    // POST api/auth/refresh-access-token
    [HttpPost("refresh-access-token")]
    public async Task<IActionResult> RefreshAccessToken([FromBody] RefreshRequest model)
    {
        try
        {
            var cognitoUserPool = new CognitoUserPool(_config["UserPoolId"], _config["AppClientId"], _cognitoClient);
            var cognitoUser = new CognitoUser(model.Username, _config["AppClientId"], cognitoUserPool, _cognitoClient);

            cognitoUser.SessionTokens = new CognitoUserSession(null, null, model.RefreshToken, DateTime.Now, DateTime.Now.AddHours(1));

            InitiateRefreshTokenAuthRequest refreshRequest = new InitiateRefreshTokenAuthRequest()
            {
                AuthFlowType = AuthFlowType.REFRESH_TOKEN_AUTH
            };

            AuthFlowResponse authResponse = await cognitoUser.StartWithRefreshTokenAuthAsync(refreshRequest);
            return Ok( new { idToken = authResponse.AuthenticationResult.IdToken });
        }

        catch (Exception exception)
        {
            return BadRequest(ExceptionHander.HandleException(exception));
        }
    }
}


