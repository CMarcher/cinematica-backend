using Cinematica.API.Data;
using Cinematica.API.Models.Database;
using Cinematica.API.Models.Display;
using Cinematica.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;
using TMDbLib.Client;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Cinematica.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PostsController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IHelperService _helper;
        private readonly string _postFiles;
        private readonly ImageSettings _imageSettings;

        public PostsController(DataContext context, IHelperService helperService, ImageSettings imageSettings)
        {
            _context = context;
            _helper = helperService;
            _imageSettings = imageSettings;
            _postFiles = Path.Combine(_imageSettings.UploadLocation, "posts");
        }

        // GET api/<PostsController>/5
        [HttpGet("{postId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPost(long postId, string? userId = null)
        {
            var post = await _context.Posts
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PostId == postId); ;

            if (post == null)
            {
                return NotFound();
            }

            //append prefix to post image if not null
            if (post.Image != null) post.Image = _imageSettings.ServeLocation + "posts/" + post.Image;

            var youLike = false;
            //Get number of replies attached to post
            var commentsCount = await _context.Replies.CountAsync(r => r.PostId == postId);
            //Get count of likes
            var likesCount = await _context.Likes.CountAsync(l => l.PostId == postId);
            //Get if logged in user likes this post
            if (userId != null) youLike = await _context.Likes.AnyAsync(l => l.PostId == postId && l.UserId == userId);
            // Get the movies for the post
            var movies = await _context.MovieSelections
                .Where(m => m.PostId == postId)
                .Select(m => DBMovie.DbMovieToSimpleMovie(m.Movie))
                .ToListAsync();

            var profilePicture = post.User.ProfilePicture;
            if (profilePicture != null)
                profilePicture = _imageSettings.ServeLocation + "users/" + profilePicture;
            return Ok(new PostDetails()
            {
                Post = post,
                UserName = post.User.UserName,
                ProfilePicture = profilePicture,
                LikesCount = likesCount,
                CommentsCount = commentsCount,
                YouLike = youLike,
                Movies = movies,
            });
        }

        // GET: api/<PostsController>/all/{page}
        [HttpGet("all/{page}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPosts(int page = 1, string? userId = null)
        {
            // Get the "page" of posts
            var posts = await _context.Posts
                .OrderByDescending(p => p.CreatedAt) // Order by creation date
                .Skip((page - 1) * 10) // Skip the posts before the current page
                .Take(10) // Take only the posts of the current page
                .ToListAsync();

            if (posts == null)
            {
                return NotFound();
            }

            var postDetailsList = new List<PostDetails>();

            foreach (var post in posts)
            {
                var postDetails = await GetPost(post.PostId, userId);
                if (postDetails is OkObjectResult okResult && okResult.Value is PostDetails details)
                {
                    postDetailsList.Add(details);
                }
            }

            // Return the paginated list of PostDetails
            return Ok(postDetailsList);
        }

        // GET: api/<PostsController>/following/{userId}/{page}
        [HttpGet("following/{userId}/{page}")]
        public async Task<IActionResult> GetFollowingPosts(string userId, int page = 1)
        {
            // checks if id token sub matches user id in request
            var valid = _helper.CheckTokenSub(HttpContext.Request.Headers["Authorization"].ToString().Split(" ")[1], userId);
            if (!valid.Item1) return Unauthorized(new { message = valid.Item2 });

            // Get the list of users that the current user is following
            var followingIds = await _context.UserFollowers
                .Where(uf => uf.FollowerId == userId)
                .Select(uf => uf.UserId)
                .ToListAsync();

            // Get the "page" of posts from the users that the current user is following
            var posts = await _context.Posts
                .Where(p => followingIds.Contains(p.UserId)) // Filter by following users
                .OrderByDescending(p => p.CreatedAt) // Order by creation date
                .Skip((page - 1) * 10) // Skip the posts before the current page
                .Take(10) // Take only the posts of the current page
                .ToListAsync();

            if (posts == null)
            {
                return NotFound();
            }

            var postDetailsList = new List<PostDetails>();

            foreach (var post in posts)
            {
                var postDetails = await GetPost(post.PostId, userId);
                if (postDetails is OkObjectResult okResult && okResult.Value is PostDetails details)
                {
                    postDetailsList.Add(details);
                }
            }

            // Return the paginated list of PostDetails
            return Ok(postDetailsList);
        }

        [HttpGet("search/{movieId}/{page}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPostsByMovie(int movieId, int page = 1)
        {
            // Get the "page" of posts that contain the specified movie
            var posts = await _context.MovieSelections
                .Where(m => m.MovieId == movieId) // Filter by movie ID
                .Select(m => m.Post) // Select the associated posts
                .OrderByDescending(p => p.CreatedAt) // Order by creation date
                .Skip((page - 1) * 10) // Skip the posts before the current page
                .Take(10) // Take only the posts of the current page
                .ToListAsync();

            if (posts == null)
            {
                return NotFound();
            }

            var postDetailsList = new List<PostDetails>();

            foreach (var post in posts)
            {
                var postDetails = await GetPost(post.PostId);
                if (postDetails is OkObjectResult okResult && okResult.Value is PostDetails details)
                {
                    postDetailsList.Add(details);
                }
            }

            // Return the paginated list of PostDetails
            return Ok(postDetailsList);
        }

        [HttpGet("{postId}/replies/{page}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetReplies(long postId, int page = 1, string? userId = null)
        {
            // Get the "page" of replies for the post
            var replies = await _context.Replies
                .Where(r => r.PostId == postId) // Filter by post ID
                .OrderByDescending(r => r.CreatedAt) // Order by creation date
                .Skip((page - 1) * 10) // Skip the replies before the current page
                .Take(10) // Take only the replies of the current page
                .ToListAsync();

            if (replies == null)
            {
                return NotFound();
            }

            var replyDetailsList = new List<ReplyDetails>();

            foreach (var reply in replies)
            {
                var youLike = false;
                if (userId != null)
                {
                    youLike = await _context.Likes.AnyAsync(l => l.ReplyId == reply.ReplyId && l.UserId == userId);
                }

                var likesCount = await _context.Likes.CountAsync(l => l.ReplyId == reply.ReplyId);
                var user = await _context.Users.FindAsync(reply.UserId);
                if (user.ProfilePicture != null)
                    user.ProfilePicture = _imageSettings.ServeLocation + "users/" + user.ProfilePicture;
                replyDetailsList.Add(new ReplyDetails()
                {
                    Reply = reply,
                    UserName = user.UserName,
                    ProfilePicture = user.ProfilePicture,
                    LikesCount = likesCount,
                    YouLike = youLike
                });
            }

            // Return the paginated list of ReplyDetails
            return Ok(replyDetailsList);
        }


        // POST api/<PostsController>
        [HttpPost]
        public async Task<IActionResult> AddPost([FromBody] AddPostModel postModel)
        {
            // checks if id token sub matches user id in request
            var valid = _helper.CheckTokenSub(HttpContext.Request.Headers["Authorization"].ToString().Split(" ")[1], postModel.NewPost.UserId);
            if (!valid.Item1) return Unauthorized(new { message = valid.Item2 });

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Posts.Add(postModel.NewPost);
            await _context.SaveChangesAsync();

            //Array of simple movies:
            List<SimpleMovie> movies = new List<SimpleMovie>();

            // Associate movies with the post
            foreach (var movieId in postModel.MovieIds)
            {
                var movieSelection = new MovieSelection
                {
                    PostId = postModel.NewPost.PostId,
                    MovieId = movieId
                };

                var dbMovie = await _context.Movies.FindAsync(movieId);
                if (dbMovie != null)
                {
                    var displayMovie = DBMovie.DbMovieToSimpleMovie(dbMovie);
                    movies.Add(displayMovie);
                }

                _context.MovieSelections.Add(movieSelection);
            }

            await _context.SaveChangesAsync();

            //Convert to PostDetails model
            var user = await _context.Users.FindAsync(postModel.NewPost.UserId);
            if (user.ProfilePicture != null)
                user.ProfilePicture = _imageSettings.ServeLocation + "users/" + user.ProfilePicture;
            //append prefix to post image if not null
            if (postModel.NewPost.Image != null) postModel.NewPost.Image = _imageSettings.ServeLocation + "posts/" + postModel.NewPost.Image;
            return Ok(new PostDetails
            {
                Post = postModel.NewPost,
                UserName = user.UserName,
                ProfilePicture = user.ProfilePicture,
                Movies = movies
            });
        }

        [HttpPost("upload")]
        [AllowAnonymous]
        public async Task<IActionResult> UploadPostImage(IFormFile imageFile)
        {
            if (imageFile == null)
            {
                return BadRequest(new { message = "No file uploaded." });
            }

            try
            {
                var fileName = await _helper.UploadFile(imageFile, _postFiles);

                // Return the new filename
                return Ok(new { FileName = fileName });
            }
            catch (Exception e)
            {
                return BadRequest(new { message = e.Message });
            }
        }

        // DELETE api/<PostsController>/5
        [HttpDelete("{postId}")]
        public async Task<IActionResult> DeletePost(long postId)
        {

            var post = await _context.Posts.FindAsync(postId);
            if (post == null)
            {
                return NotFound();
            }

            // checks if id token sub matches user id in request
            var valid = _helper.CheckTokenSub(HttpContext.Request.Headers["Authorization"].ToString().Split(" ")[1], post.UserId);
            if (!valid.Item1) return Unauthorized(new { message = valid.Item2 });

            // Remove the associated movie selections
            var movieSelections = _context.MovieSelections.Where(m => m.PostId == postId);
            _context.MovieSelections.RemoveRange(movieSelections);

            // Remove the associated replies
            var replies = _context.Replies.Where(r => r.PostId == postId);
            _context.Replies.RemoveRange(replies);

            // Remove the associated likes
            var likes = _context.Likes.Where(l => l.PostId == postId);
            _context.Likes.RemoveRange(likes);

            // Remove the post
            _context.Posts.Remove(post);

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/<PostsController>/like/{userId}/{likeId}
        [HttpPut("like/{userId}/{postId}")]
        public async Task<IActionResult> LikePost(long postId, string userId)
        {
            // checks if id token sub matches user id in request
            var valid = _helper.CheckTokenSub(HttpContext.Request.Headers["Authorization"].ToString().Split(" ")[1], userId);
            if (!valid.Item1) return Unauthorized(new { message = valid.Item2 });

            var like = await _context.Likes.FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

            if (like == null)
            {
                // If the like doesn't exist, create it
                like = new Like
                {
                    PostId = postId,
                    UserId = userId
                };

                _context.Likes.Add(like);
            }
            else
            {
                // If the like exists, remove it
                _context.Likes.Remove(like);
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        public class AddPostModel
        {
            public Post NewPost { get; set; }
            public int[] MovieIds { get; set; }
        }
    }
}
