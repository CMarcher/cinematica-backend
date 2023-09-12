using Cinematica.API.Data;
using Cinematica.API.Models.Database;
using Cinematica.API.Models.Display;
using Cinematica.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDbLib.Client;

namespace Cinematica.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RepliesController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IHelperService _helper;

        public RepliesController(DataContext context, IHelperService helperService)
        {
            _context = context;
            _helper = helperService;
        }

        // GET api/<RepliesController>/1
        [HttpGet("{id}")]
        public async Task<IActionResult> GetReply(long id)
        {
            var reply = await _context.Replies.FindAsync(id);

            if (reply == null)
            {
                return NotFound();
            }

            var likesCount = await _context.Likes.CountAsync(l => l.ReplyId == id);

            var replyDetails = new
            {
                Reply = reply,
                LikesCount = likesCount,
            };

            return Ok(replyDetails);
        }

        // POST api/<RepliesController>
        [HttpPost]
        public async Task<IActionResult> CreateReply([FromBody] Reply model)
        {
            try
            {
                _context.Replies.Add(model);
                await _context.SaveChangesAsync();

                return Ok(model);
            }
            catch (Exception e)
            {
                return BadRequest(new { message = e.ToString() });
            }
        }

        // POST api/<RepliesController>/1
        [HttpPost("like")]
        public IActionResult LikeReply([FromBody] Like model)
        {
            try
            {
                if(model.PostId == null && model.ReplyId != null)
                {
                    _context.Likes.Add(model);
                    _context.SaveChanges();
                    return Ok(new { message = "Successfully liked reply." });
                }
                

                return BadRequest(new { message = "PostId must be null and ReplyId must have a value." });
            }
            catch (Exception e)
            {
                return BadRequest(new { message = e.ToString() });
            }
        }

        // DELETE api/<RepliesController>/1
        [HttpDelete("{id}")]
        public IActionResult DeleteReply(long id)
        {
            try
            {
                var likedReplies = _context.Likes.Where(l => l.ReplyId == id);

                foreach (var like in likedReplies)
                {
                    _context.Likes.Remove(like);
                }

                _context.Replies.Remove(new Reply() { ReplyId = id });
                _context.SaveChangesAsync();

                return Ok(new { message = "Successfully removed reply." });
            }
            catch (Exception e)
            {
                return BadRequest(new { message = e.ToString() });
            }
        }

        // Post api/<RepliesController>/unlike
        [HttpPost("unlike")]
        public IActionResult UnlikeReply([FromBody] Like model)
        {
            try
            {
                // gets the like based on like_id and checks if the like is for a reply
                var like = _context.Likes.Find(model.LikeId);
                if (like.PostId == null && like.ReplyId != null)
                {
                    _context.Remove(like);
                    _context.SaveChanges();
                    return Ok(new { message = "Successfully unliked reply." });
                }

                return BadRequest(new { message = "The supplied like_id is not for a reply." });
            }
            catch (Exception e)
            {
                return BadRequest(new { message = e.ToString() });
            }
        }
    }
}
