using Microsoft.AspNetCore.Mvc;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Cinematica.API.Models.Cognito;
using Amazon.Extensions.CognitoAuthentication;
using Amazon;

namespace Cinematica.API.Controllers;

[Route("api/[controller]")]
public class CognitoController : ControllerBase
{
    private readonly IConfiguration APP_CONFIG;
    private AmazonCognitoIdentityProviderClient cognitoIdClient;

    public CognitoController(IConfiguration config) {
        APP_CONFIG = config.GetSection("AWS");

        cognitoIdClient = new AmazonCognitoIdentityProviderClient
        (
            APP_CONFIG.GetValue<string>("AccessKeyId"), 
            APP_CONFIG.GetValue<string>("AccessSecretKey"), 
            RegionEndpoint.GetBySystemName(APP_CONFIG.GetValue<string>("Region"))
        );
    }
    
    // POST api/cognito/register
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest model)
    {
        try {      
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
        catch(Exception e) {
            return BadRequest(new { message = e.ToString().Split("\r\n")[0] });
        }
    }

    // POST api/cognito/confirm_registration
    [HttpPost("confirm_registration")]
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
            return Ok(new { message = "Verification successful." });;
        }
        catch(Exception e) {
            return BadRequest(new { message = e.ToString().Split("\r\n")[0] });
        }
    }

    // POST api/cognito/login
    [HttpPost("login")]    
    public async Task<IActionResult> Login(LoginRequest model)
    {
        try
        {
            var cognitoUserPool = new CognitoUserPool(APP_CONFIG.GetValue<String>("UserPoolId"), APP_CONFIG.GetValue<String>("AppClientId"), cognitoIdClient);
            var cognitoUser = new CognitoUser(model.Username, APP_CONFIG.GetValue<String>("AppClientId"), cognitoUserPool, cognitoIdClient);
        
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
}