using Cinematica.API.Data;
using Cinematica.API.Models.Database;
using Cinematica.API.Models.Display;
using Cinematica.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDbLib.Client;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Cinematica.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostsController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IHelperService _helper;
        private readonly string _postFiles;

        public PostsController(DataContext context, IHelperService helperService, string myImages)
        {
            _context = context;
            _helper = helperService;
            _postFiles = Path.Combine(myImages, "posts");
        }

        // GET: api/<PostsController>/all/{page}
        [HttpGet("all/{page}")]
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
            else
            {
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
        }

        // GET: api/<PostsController>/following/{userId}/{page}
        [HttpGet("following/{userId}/{page}")]
        public async Task<IActionResult> GetFollowingPosts(string userId, int page = 1)
        {
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
            else
            {
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
        }

        [HttpGet("search/{movieId}/{page}")]
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
            else
            {
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
        }

        // GET api/<PostsController>/5
        [HttpGet("{id}/{userId?}")]
        public async Task<IActionResult> GetPost(long id, string? userId = null)
        {
            var post = await _context.Posts.FindAsync(id);
            var youLike = false;

            if (post == null)
            {
                return NotFound();
            }

            var commentsCount = await _context.Replies.CountAsync(r => r.PostId == id);
            var likesCount = await _context.Likes.CountAsync(l => l.PostId == id);
            if (userId != null)
            {
                youLike = await _context.Likes.AnyAsync(l => l.PostId == id && l.UserId == userId);
            }
            

            // Get the movies for the post
            var movies = await _context.MovieSelections
                .Where(m => m.PostId == id)
                .Select(m => DBMovie.DbMovieToSimpleMovie(m.Movie))
                .ToListAsync();

            var postDetails = new PostDetails
            {
                Post = post,
                CommentsCount = commentsCount,
                LikesCount = likesCount,
                Movies = movies,
                youLike = youLike
            };

            return Ok(postDetails);
        }

        // POST api/<PostsController>
        [HttpPost]
        public async Task<IActionResult> AddPost([FromForm] Post newPost, [FromForm] int[] movieIds, [FromForm] IFormFile? imageFile = null)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Upload the image file and get the filename
            if (imageFile != null)
            {
                var fileName = await _helper.UploadFile(imageFile, _postFiles);

                // Set the Image property of the new post
                newPost.Image = fileName;
            }

            _context.Posts.Add(newPost);
            await _context.SaveChangesAsync();

            // Associate movies with the post
            foreach (var movieId in movieIds)
            {
                var movieSelection = new MovieSelection
                {
                    PostId = newPost.PostId,
                    MovieId = movieId
                };

                _context.MovieSelections.Add(movieSelection);
            }

            await _context.SaveChangesAsync();

            return Ok(newPost);
        }

        // PUT: api/<PostsController>/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePost(long id, [FromForm] Post updatedPost, [FromForm] IFormFile? imageFile = null, [FromForm] int[]? movieIds = null)
        {
            if (id != updatedPost.PostId)
            {
                return BadRequest();
            }

            // Upload the image file and get the filename
            if (imageFile != null)
            {
                var fileName = await _helper.UploadFile(imageFile, _postFiles);

                // Set the Image property of the updated post
                updatedPost.Image = fileName;
            }

            _context.Entry(updatedPost).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PostExists(id).Result)
                {
                    return NotFound();
                }

                throw;
            }

            // Update the movies associated with the post
            if (movieIds == null) return NoContent();
            // Remove existing associations
            var existingSelections = _context.MovieSelections.Where(m => m.PostId == id);
            _context.MovieSelections.RemoveRange(existingSelections);

            // Add new associations
            foreach (var movieId in movieIds)
            {
                var movieSelection = new MovieSelection
                {
                    PostId = updatedPost.PostId,
                    MovieId = movieId
                };

                _context.MovieSelections.Add(movieSelection);
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }


        // DELETE api/<PostsController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePost(long id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            // Remove the associated movie selections
            var movieSelections = _context.MovieSelections.Where(m => m.PostId == id);
            _context.MovieSelections.RemoveRange(movieSelections);

            // Remove the associated replies
            var replies = _context.Replies.Where(r => r.PostId == id);
            _context.Replies.RemoveRange(replies);

            // Remove the associated likes
            var likes = _context.Likes.Where(l => l.PostId == id);
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

        // Helper to see if a Post exists
        private async Task<bool> PostExists(long id)
        {
            return await _context.Posts.AnyAsync(e => e.PostId == id);
        }
    }
}
