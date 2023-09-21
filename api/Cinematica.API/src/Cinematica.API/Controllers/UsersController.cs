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
    [Route("[controller]")]
    [ApiController]
    [Authorize]
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
        [AllowAnonymous]
        public async Task<IActionResult> GetUser(string userId)
        {
            // get user from postgre database
            var databaseUser = await _context.Users.SingleOrDefaultAsync(u => u.UserId.Equals(userId));

            if (databaseUser is null)
                return NotFound($"The user with ID {userId} could not be found.");

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

        // POST api/<UsersController>/follow
        [HttpPost("follow")]
        public async Task<IActionResult> Follow([FromBody] UserFollower model)
        {
            // checks if id token sub matches user id in request
            var valid = _helper.CheckTokenSub(HttpContext.Request.Headers["Authorization"].ToString(), model.FollowerId);
            if (!valid.Item1) 
                return Unauthorized(new { message = valid.Item2 });

            await _context.AddAsync(new UserFollower { UserId = model.UserId, FollowerId = model.FollowerId });
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Follow success." });
        }

        // POST api/<UsersController>/unfollow
        [HttpPost("unfollow")]
        public async Task<IActionResult> Unfollow([FromBody] UserFollower model)
        {
            // checks if id token sub matches user id in request
            var valid = _helper.CheckTokenSub(HttpContext.Request.Headers["Authorization"].ToString(), model.FollowerId);
            if (!valid.Item1) 
                return Unauthorized(new { message = valid.Item2 });

            _context.Remove(new UserFollower { UserId = model.UserId, FollowerId = model.FollowerId });
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Unfollow success." });
        }

        // GET api/<UsersController>/followers/id
        [HttpGet("followers/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetFollowers(string id)
        {
            var followersId = await _context.UserFollowers
                                .Where(u => u.UserId.Contains(id))
                                .Select(p => p.FollowerId)
                                .ToListAsync();

            List<dynamic> followers = new List<dynamic>();

            foreach (var fid in followersId)
            {
                var user = await _context.Users.FindAsync(fid);

                followers.Add(new
                {
                    UserId = fid,
                    Username = user.UserName
                });
            }

            return Ok(followers);
        }

        // GET api/<UsersController>/following/id
        [HttpGet("following/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetFollowing(string id)
        {
            var followingIds = await _context.UserFollowers
                                .Where(u => u.FollowerId.Contains(id))
                                .Select(p => p.UserId)
                                .ToListAsync();

            List<dynamic> following = new List<dynamic>();

            foreach (var fid in followingIds)
            {
                var user = await _context.Users.FindAsync(fid);

                following.Add(new
                {
                    UserId = fid,
                    Username = user.UserName
                });
            }

            return Ok(following);
        }

        // POST api/<UsersController>/add-movie
        [HttpPost("add-movie")]
        public async Task<IActionResult> AddMovie([FromBody] UserMovie model)
        {
            // checks if id token sub matches user id in request
            var valid = _helper.CheckTokenSub(HttpContext.Request.Headers["Authorization"].ToString(), model.UserId);
            if (!valid.Item1) 
                return Unauthorized(new { message = valid.Item2 });

            await _context.AddAsync(model);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Movie successfully added to user." });
        }

        // POST api/<UsersController>/remove-movie
        [HttpPost("remove-movie")]
        public async Task<IActionResult> RemoveMovie([FromBody] UserMovie model)
        {
            // checks if id token sub matches user id in request
            var valid = _helper.CheckTokenSub(HttpContext.Request.Headers["Authorization"].ToString(), model.UserId);
            if (!valid.Item1) 
                return Unauthorized(new { message = valid.Item2 });

            _context.Remove(model);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Successfully removed movie from user." });
        }

        // GET api/<UsersController>/movies/id
        [HttpGet("movies/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUserMovies(string id)
        {
            // gets the movie ids that a user has
            var movies = await _context.UserMovies
                                .Where(u => u.UserId.Contains(id))
                                .Select(p => p.MovieId)
                                .ToListAsync();

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

        // GET api/<UsersController>/posts/id
        [HttpGet("posts/{userId}/{page}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUserPosts(string userId, int page)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null) 
                return BadRequest(new { message = "No such user" });
            
            if (user.ProfilePicture != null)
                user.ProfilePicture = _imageSettings.ServeLocation + "users/" + user.ProfilePicture;

            var posts = await _context.Posts
                        .Where(p => p.UserId.Contains(userId))
                        .OrderByDescending(p => p.CreatedAt)
                        .Skip((page - 1) * 10)
                        .Take(10)
                        .ToListAsync();

            if (posts is null)
                return Ok(new List<Post>());

            // creates a list of anonymous objects to store the replies to return to client
            List<PostDetails> postsList = new List<PostDetails>();

            foreach (var post in posts)
            {
                var likesCount = _context.Likes.Count(l => l.PostId == post.PostId);
                var youLike = _context.Likes.Any(l => l.PostId == post.PostId && l.UserId == userId);
                var commentsCount = _context.Replies.Count(r => r.PostId == post.PostId);

                //append prefix to post image if not null
                if (post.Image != null) post.Image = _imageSettings.ServeLocation + "posts/" + post.Image;

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
        [AllowAnonymous]
        public async Task<IActionResult> GetUserReplies(string id, int page)
        {
            var replies = await _context.Replies
                                .Where(u => u.UserId.Contains(id))
                                .OrderByDescending(p => p.CreatedAt)
                                .Skip((page - 1) * 10)
                                .Select(r => new { r.ReplyId, r.PostId, r.Body, r.CreatedAt })
                                .Take(10)
                                .ToListAsync();

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

        // GET api/<UsersController>/replies/id
        [HttpGet("likes/{id}/{page}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUserLikes(string id, int page)
        {
            var likes = await _context.Likes
                                .Where(u => u.UserId.Contains(id))
                                .OrderByDescending(l => l.LikeId)
                                .Skip((page - 1) * 10)
                                .Select(p => new { p.LikeId, p.PostId, p.ReplyId })
                                .Take(10)
                                .ToListAsync();

            // creates a list of anonymous objects to store the replies to return to client
            List<dynamic> likedPosts = new List<dynamic>();

            foreach (var like in likes)
            {
                if (like.ReplyId != null)
                {
                    var likedReply = await _context.Replies.FindAsync(like.ReplyId);
                    var likesCount = await _context.Likes.CountAsync(l => l.ReplyId == like.ReplyId);
                    var replyUser = await _context.Users.FindAsync(likedReply.UserId);

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
                    var likedPost = await _context.Posts.FindAsync(like.PostId);
                    var likesCount = await _context.Likes.CountAsync(l => l.ReplyId == like.ReplyId);
                    var postUser = await _context.Users.FindAsync(likedPost.UserId);

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

        // POST api/<UsersController>/set-profile-picture
        [HttpPost("set-profile-picture")]
        public async Task<IActionResult> SetProfilePicture([FromForm] SetPictureRequest model)
        {
            // checks if id token sub matches user id in request
            var valid = _helper.CheckTokenSub(HttpContext.Request.Headers["Authorization"].ToString(), model.UserId);
            if (!valid.Item1) 
                return Unauthorized(new { message = valid.Item2 });

            var user = await _context.Users.FindAsync(model.UserId);
            var filepath = await _helper.UploadFile(model.File, _usersFiles);
            user.ProfilePicture = filepath;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Profile picture successfully set." });
        }

        // POST api/<UsersController>/set-cover-picture
        [HttpPost("set-cover-picture")]
        public async Task<IActionResult> SetCoverPicture([FromForm] SetPictureRequest model)
        {
            // checks if id token sub matches user id in request
            var valid = _helper.CheckTokenSub(HttpContext.Request.Headers["Authorization"].ToString(), model.UserId);
            if (!valid.Item1) 
                return Unauthorized(new { message = valid.Item2 });

            var user = await _context.Users.FindAsync(model.UserId);
            var filepath = await _helper.UploadFile(model.File, _usersFiles);
            user.CoverPicture = filepath;
            
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cover picture successfully set." });
        }
    }
}