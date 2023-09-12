using Cinematica.API.Data;
using Cinematica.API.Models.Database;
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
        public async Task<IActionResult> GetPosts(int page = 1)
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
                // Return the paginated list of posts
                return Ok(posts);
            }
        }

        // GET api/<PostsController>/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPost(long id)
        {
            var post = await _context.Posts.FindAsync(id);

            if (post == null)
            {
                return NotFound();
            }

            return Ok(post);
        }

        // POST api/<PostsController>
        [HttpPost]
        public async Task<IActionResult> AddPost([FromBody] Post newPost)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Posts.Add(newPost);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPost), new { id = newPost.PostId }, newPost);
        }


        // PUT api/<PostsController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<PostsController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
