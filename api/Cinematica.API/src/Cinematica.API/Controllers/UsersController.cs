using Cinematica.API.Data;
using Cinematica.API.Models.User;
using Cinematica.API.Models.Database;
using Cinematica.API.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;
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
        private readonly IHelperService _helper;
        private DataContext _context;
        private AmazonCognitoIdentityProviderClient cognitoIdClient;
        private readonly string _usersFiles;

        public UsersController(IConfiguration config, IHelperService helperService, DataContext context, string myImages)
        {
            APP_CONFIG = config.GetSection("AWS");

            _context = context;
            _helper = helperService;
            _usersFiles = Path.Combine(myImages, "users");

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
                var cognitoUser = await _helper.GetCognitoUser(id);

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
        public IActionResult Follow([FromBody] UserFollower model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

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
        public IActionResult Unfollow([FromBody] UserFollower model)
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
                                    .Select(p => new { p.FollowerId, _helper.GetCognitoUser(p.FollowerId).Result.Username });

                return Ok(followers);
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
                                    .Select(p => new { p.UserId, _helper.GetCognitoUser(p.UserId).Result.Username });

                return Ok(following);
            }
            catch (Exception e)
            {
                return BadRequest(new { message = e.ToString() });
            }
        }

        // POST api/<UsersController>/add-movie
        [HttpPost("add-movie")]
        public IActionResult AddMovie([FromBody] UserMovie model)
        {
            try
            {
                _context.Add(model);
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
        public IActionResult RemoveMovie([FromBody] UserMovie model)
        {
            try
            {
                _context.Remove(model);
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
                // gets the movie ids that a user has
                var movies = _context.UserMovies
                                    .Where(u => u.UserId.Contains(id))
                                    .Select(p => p.MovieId)
                                    .ToList();

                // creates a list of anonymous objects containg simple movie details to return to client
                List<dynamic> simpleMoviesList = new List<dynamic>();

                foreach (var movie in movies)
                {
                    var dbMovie = _context.Movies.Find(movie);

                    simpleMoviesList.Add( new
                    {
                        Id = movie,
                        Title = dbMovie.Title,
                        ReleaseYear = dbMovie.ReleaseDate?.Year.ToString()
                    });
                }

                return Ok(simpleMoviesList);
            }
            catch (Exception e)
            {
                return BadRequest(new { message = e.ToString() });
            }
        }

        // GET api/<UsersController>/posts/id
        [HttpGet("posts/{id}/{page}")]
        public IActionResult GetUserPosts(string id, int page)
        {
            try
            {
                var posts = _context.Posts
                                    .Where(u => u.UserId.Contains(id))
                                    .OrderByDescending(p => p.CreatedAt)
                                    .Skip((page - 1) * 10)     
                                    .Select(p => new { p.PostId })
                                    .Take(10);

                return Ok(posts);
            }
            catch (Exception e)
            {
                return BadRequest(new { message = e.ToString() });
            }
        }

        // GET api/<UsersController>/replies/id
        [HttpGet("replies/{id}/{page}")]
        public IActionResult GetUserReplies(string id, int page)
        {
            try
            {
                var replies = _context.Replies
                                    .Where(u => u.UserId.Contains(id))
                                    .OrderByDescending(p => p.CreatedAt)
                                    .Skip((page - 1) * 10)
                                    .Select(r => new { r.ReplyId })
                                    .Take(10);

                return Ok(replies);
            }
            catch (Exception e)
            {
                return BadRequest(new { message = e.ToString() });
            }
        }

        // GET api/<UsersController>/replies/id
        [HttpGet("likes/{id}/{page}")]
        public IActionResult GetUserLikes(string id, int page)
        {
            try
            {
                var likes = _context.Likes
                                    .Where(u => u.UserId.Contains(id))
                                    .OrderByDescending(l => l.LikeId)
                                    .Skip((page - 1) * 10)
                                    .Select(p => new { p.LikeId, p.PostId, p.ReplyId })
                                    .Take(10);

                return Ok(likes);
            }
            catch (Exception e)
            {
                return BadRequest(new { message = e.ToString() });
            }
        }

        // POST api/<UsersController>/set-profile-picture
        [HttpPost("set-profile-picture")]
        public async Task<IActionResult> SetProfilePicture([FromForm] SetPictureRequest model)
        {
            try
            {
                var user = await _context.Users.FindAsync(model.UserId);
                var filepath = _helper.UploadFile(model.File, _usersFiles).Result;
                user.ProfilePicture = filepath;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Profile picture successfully set." });
            }
            catch (Exception e)
            {
                return BadRequest(new { message = e.ToString() });
            }
        }

        // POST api/<UsersController>/set-cover-picture
        [HttpPost("set-cover-picture")]
        public async Task<IActionResult> SetCoverPicture([FromForm] SetPictureRequest model)
        {
            try
            {
                var user = await _context.Users.FindAsync(model.UserId);
                var filepath = _helper.UploadFile(model.File, _usersFiles).Result;
                user.CoverPicture = filepath;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Cover picture successfully set." });
            }
            catch (Exception e)
            {
                return BadRequest(new { message = e.ToString() });
            }
        }
    }
}