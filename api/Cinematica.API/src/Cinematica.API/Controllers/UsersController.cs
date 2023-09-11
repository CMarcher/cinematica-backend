using Cinematica.API.Data;
using Cinematica.API.Models;
using Cinematica.API.Models.Database;
using Microsoft.AspNetCore.Mvc;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Extensions.CognitoAuthentication;
using Amazon;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Cinematica.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IConfiguration APP_CONFIG;
        private DataContext _context;
        private AmazonCognitoIdentityProviderClient cognitoIdClient;

        public UsersController(IConfiguration config, DataContext context)
        {
            _context = context;

            APP_CONFIG = config.GetSection("AWS");

            cognitoIdClient = new AmazonCognitoIdentityProviderClient
            (
                APP_CONFIG["AccessKeyId"],
                APP_CONFIG["AccessSecretKey"],
                RegionEndpoint.GetBySystemName(APP_CONFIG["Region"])
            );
        }

        // GET api/Users/username
        [HttpGet("{username}")]
        public async Task<IActionResult> GetUser(string username)
        {
            try
            {
                // get user from cognito
                var getRequest = new AdminGetUserRequest()
                {
                    UserPoolId = APP_CONFIG["UserPoolId"],
                    Username = username,
                };
                var cognitoUser = await cognitoIdClient.AdminGetUserAsync(getRequest);
                var userId = cognitoUser.UserAttributes.ToArray()[0].Value;

                // get user from postgre database
                var databaseUser = _context.Users.SingleOrDefault(u => u.UserId.Equals(userId));

                // get follower and following count
                var follower_count = _context.UserFollowers.Count(u => u.UserId == userId);
                var following_count = _context.UserFollowers.Count(u => u.FollowerId == userId);

                return Ok(new { 
                    id = cognitoUser.UserAttributes.ToArray()[0].Value,
                    profile_picture = databaseUser.ProfilePicture,
                    cover_picture = databaseUser.CoverPicture,
                    follower_count = follower_count ,
                    following_count = following_count,

                });
            }
            catch(Exception UserUserNotFoundException)
            {
                return BadRequest(new { message = "User not found." });
            }
            catch(Exception e)
            {
                return BadRequest(new { message = e.ToString() });
            }

            /*
            // Get User details from database
            var user = _context.Users.SingleOrDefault(u => u.UserId.Equals(id));
            if (user == null)
            {
                return NotFound();
            }
            else
            {
                return Ok(user);
            }
            */
        }
        
        // POST api/<UsersController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<UsersController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<UsersController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
