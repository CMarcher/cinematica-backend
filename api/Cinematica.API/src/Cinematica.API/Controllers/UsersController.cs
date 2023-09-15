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
using Cinematica.API.Models.Display;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Cinematica.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IHelperService _helper;
        private DataContext _context;
        private readonly string _usersFiles;
        private readonly ImageSettings _imageSettings;

        public UsersController(IConfiguration config, IHelperService helperService, DataContext context, ImageSettings imageSettings)
        {
            _context = context;
            _helper = helperService;
            _imageSettings = imageSettings;
            _usersFiles = Path.Combine(_imageSettings.UploadLocation, "users");
        }

        // GET api/Users/id
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUser(string userId)
        {
            try
            {
                // get user from postgre database
                var databaseUser = await _context.Users.SingleOrDefaultAsync(u => u.UserId.Equals(userId));

                // get follower and following count
                var follower_count = _context.UserFollowers.Count(u => u.UserId == userId);
                var following_count = _context.UserFollowers.Count(u => u.FollowerId == userId);

                return Ok(new
                {
                    userId,
                    username = databaseUser.UserName,
                    profile_picture = _imageSettings.ServeLocation + "users/" + databaseUser.ProfilePicture,
                    cover_picture = _imageSettings.ServeLocation + "users/" + databaseUser.CoverPicture,
                    follower_count,
                    following_count,
                });
            }
            catch (UserNotFoundException)
            {
                return BadRequest(new { message = "User not found." });
            }
            catch (Exception e)
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
                _context.Add(new UserFollower { UserId = model.UserId, FollowerId = model.FollowerId });
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

                // creates a list of anonymous objects to store simple movie details to return to client
                List<dynamic> simpleMoviesList = new List<dynamic>();

                foreach (var movie in movies)
                {
                    var dbMovie = _context.Movies.Find(movie);

                    simpleMoviesList.Add(new
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
        [HttpGet("posts/{userId}/{page}")]
        public async Task<IActionResult> GetUserPosts(string userId, int page)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null) return BadRequest(new { message = "User such user" });

            var posts = await _context.Posts
                        .Where(p => p.UserId.Contains(userId))
                        .OrderByDescending(p => p.CreatedAt)
                        .Skip((page - 1) * 10)
                        .Take(10)
                        .ToListAsync();

            if (posts == null) return BadRequest(new { message = "No posts found" });

            // creates a list of anonymous objects to store the replies to return to client
            List<PostDetails> postsList = new List<PostDetails>();

            foreach (var post in posts)
            {
                var likesCount = _context.Likes.Count(l => l.PostId == post.PostId);
                var youLike = _context.Likes.Any(l => l.PostId == post.PostId && l.UserId == userId);
                var commentsCount = _context.Replies.Count(r => r.PostId == post.PostId);

                // Get the movies for the post
                var movies = _context.MovieSelections
                    .Where(m => m.PostId == post.PostId)
                    .Select(m => DBMovie.DbMovieToSimpleMovie(m.Movie))
                    .ToList();

                postsList.Add(new PostDetails
                {
                    Post = post,
                    UserName = user.UserName,
                    ProfilePicture = user.ProfilePicture,
                    Movies = movies,
                    LikesCount = likesCount,
                    CommentsCount = commentsCount,
                    YouLike = youLike
                });
            }

            return Ok(postsList);
        }

        // GET api/<UsersController>/replies/id
        [HttpGet("replies/{id}/{page}/")]
        public IActionResult GetUserReplies(string id, int page)
        {
            try
            {
                var replies = _context.Replies
                                    .Where(u => u.UserId.Contains(id))
                                    .OrderByDescending(p => p.CreatedAt)
                                    .Skip((page - 1) * 10)
                                    .Select(r => new { r.ReplyId, r.PostId, r.Body, r.CreatedAt })
                                    .Take(10)
                                    .ToList();

                // creates a list of anonymous objects to store the replies to return to client
                List<dynamic> repliesList = new List<dynamic>();

                foreach (var reply in replies)
                {
                    var likesCount = _context.Likes.Count(l => l.ReplyId == reply.ReplyId);
                    var youLike = _context.Likes.Any(l => l.ReplyId == reply.ReplyId && l.UserId == id);


                    repliesList.Add(new
                    {
                        Reply = reply,
                        LikesCount = likesCount,
                        YouLike = youLike
                    });
                }
                return Ok(repliesList);
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
                                    .Take(10)
                                    .ToList();

                // creates a list of anonymous objects to store the replies to return to client
                List<dynamic> likedPosts = new List<dynamic>();

                foreach (var like in likes)
                {
                    if (like.ReplyId != null)
                    {
                        var likedReply = _context.Replies.Find(like.ReplyId);
                        var likesCount = _context.Likes.Count(l => l.ReplyId == like.ReplyId);
                        var replyUser = _context.Users.Find(likedReply.UserId);

                        likedPosts.Add(new
                        {
                            replyUser = replyUser.UserName,
                            replyProfilePicture = replyUser.ProfilePicture,
                            replyId = likedReply.ReplyId,
                            replyBody = likedReply.Body,
                            createdAt = likedReply.CreatedAt,
                            likesCount = likesCount
                        });
                    }
                    else
                    {
                        var likedPost = _context.Posts.Find(like.PostId);
                        var likesCount = _context.Likes.Count(l => l.ReplyId == like.ReplyId);
                        var postUser = _context.Users.Find(likedPost.UserId);

                        likedPosts.Add(new
                        {
                            postUser = postUser.UserName,
                            postProfilePicture = postUser.ProfilePicture,
                            postId = likedPost.PostId,
                            postBody = likedPost.Body,
                            image = likedPost.Image,
                            created_at = likedPost.CreatedAt,
                            likesCount = likesCount
                        });
                    }
                }

                return Ok(likedPosts);
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
                var filepath = await _helper.UploadFile(model.File, _usersFiles);
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
                var filepath = await _helper.UploadFile(model.File, _usersFiles);
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