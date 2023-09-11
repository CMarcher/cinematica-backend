using Cinematica.API.Data;
using Cinematica.API.Models.User;
using Cinematica.API.Models.Database;
using Microsoft.AspNetCore.Mvc;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Extensions.CognitoAuthentication;
using Amazon;
using System.Net;

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

        // GET api/Users/id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
            try
            {
                // get user from cognito
                var cognitoUser = await GetCognitoUser(id);

                // get user from postgre database
                var databaseUser = _context.Users.SingleOrDefault(u => u.UserId.Equals(id));

                // get follower and following count
                var follower_count = _context.UserFollowers.Count(u => u.UserId == id);
                var following_count = _context.UserFollowers.Count(u => u.FollowerId == id);

                return Ok(new { 
                    id = id,
                    username = cognitoUser.Username,
                    profile_picture = databaseUser.ProfilePicture,
                    cover_picture = databaseUser.CoverPicture,
                    follower_count = follower_count ,
                    following_count = following_count,
                });
            }
            catch(UserNotFoundException)
            {
                return BadRequest(new { message = "User not found." });
            }
            catch(Exception e)
            {
                return BadRequest(new { message = e.ToString() });
            }
        }

        // POST api/<UsersController>/follow
        [HttpPost("follow")]
        public IActionResult Follow([FromBody] FollowRequest model)
        {
            try
            {
                _context.Add(new UserFollower { UserId = model.UserId, FollowerId = model.FollowerId});
                _context.SaveChanges();
                return Ok(new { message = "Follow success." });
            }
            catch (Exception e)
            {
                return BadRequest(new { message = e.ToString() });
            }
        }

        // POST api/<UsersController>/unfollow
        [HttpPost("unfollow")]
        public IActionResult Unfollow([FromBody] FollowRequest model)
        {
            try
            {
                _context.Remove(new UserFollower { UserId = model.UserId, FollowerId = model.FollowerId });
                _context.SaveChanges();
                return Ok(new { message = "Unfollow success." });
            }
            catch (Exception e)
            {
                return BadRequest(new { message = e.ToString() });
            }
        }

        // GET api/<UsersController>/followers/id
        [HttpGet("followers/{id}")]
        public IActionResult GetFollowers(string id)
        {
            try
            {
                var followers = _context.UserFollowers
                                    .Where(u => u.UserId.Contains(id))
                                    .Select(p => new { p.FollowerId });

                return Ok(new { followers = followers });
            }
            catch (Exception e)
            {
                return BadRequest(new { message = e.ToString() });
            }
        }

        // GET api/<UsersController>/following/id
        [HttpGet("following/{id}")]
        public IActionResult GetFollowing(string id)
        {
            try
            {
                var following = _context.UserFollowers
                                    .Where(u => u.FollowerId.Contains(id))
                                    .Select(p => new { p.UserId });

                return Ok(new { following = following });
            }
            catch (Exception e)
            {
                return BadRequest(new { message = e.ToString() });
            }
        }

        // POST api/<UsersController>/add-movie
        [HttpPost("add-movie")]
        public IActionResult AddMovie([FromBody] MovieRequest model)
        {
            try
            {
                _context.Add(new UserMovie { UserId = model.UserId, MovieId = model.MovieId });
                _context.SaveChanges();
                return Ok(new { message = "Movie successfully added to user." });
            }
            catch (Exception e)
            {
                return BadRequest(new { message = e.ToString() });
            }
        }

        // POST api/<UsersController>/remove-movie
        [HttpPost("remove-movie")]
        public IActionResult RemoveMovie([FromBody] MovieRequest model)
        {
            try
            {
                _context.Remove(new UserMovie { UserId = model.UserId, MovieId = model.MovieId });
                _context.SaveChanges();
                return Ok(new { message = "Successfully removed movie from user." });
            }
            catch (Exception e)
            {
                return BadRequest(new { message = e.ToString() });
            }
        }

        // GET api/<UsersController>/movies/id
        [HttpGet("movies/{id}")]
        public IActionResult GetUserMovies(string id)
        {
            try
            {
                var movies = _context.UserMovies
                                    .Where(u => u.UserId.Contains(id))
                                    .Select(p => new { p.MovieId });

                return Ok(new { movies = movies });
            }
            catch (Exception e)
            {
                return BadRequest(new { message = e.ToString() });
            }
        }

        // POST api/<UsersController>/set-profile-picture
        [HttpPost("set-profile-picture")]
        public IActionResult SetProfilePicture([FromBody] SetPictureRequest model)
        {
            try
            {
                var user = _context.Users.SingleOrDefault(u => u.UserId == model.UserId);
                user.ProfilePicture = model.Picture;
                _context.SaveChanges();
                return Ok(new { message = "Profile picture successfully set." });
            }
            catch (Exception e)
            {
                return BadRequest(new { message = e.ToString() });
            }
        }

        // POST api/<UsersController>/set-cover-picture
        [HttpPost("set-cover-picture")]
        public IActionResult SetCoverPicture([FromBody] SetPictureRequest model)
        {
            try
            {
                var user = _context.Users.SingleOrDefault(u => u.UserId == model.UserId);
                user.CoverPicture = model.Picture;
                _context.SaveChanges();
                return Ok(new { message = "Cover picture successfully set." });
            }
            catch (Exception e)
            {
                return BadRequest(new { message = e.ToString() });
            }
        }

        // Helper function to find a cognito user by id
        private async Task<UserType?> GetCognitoUser(string id)
        {
            ListUsersRequest listUsersRequest = new ListUsersRequest
            {
                UserPoolId = APP_CONFIG["UserPoolId"],
                Filter = "sub = \"" + id + "\""
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
}