using Cinematica.API.Data;
using Cinematica.API.Models.Database;
using Cinematica.API.Models.Display;
using Cinematica.API.Services;
using Microsoft.AspNetCore.Authorization;
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

        // POST api/<RepliesController>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateReply([FromBody] Reply model)
        {
            try
            {
                var tokenString = HttpContext.Request.Headers["Authorization"].ToString().Split(" ")[1];

                if(!await _helper.CheckTokenSub(tokenString, model.UserId))
                {
                    return Unauthorized(new { message = "Sub in the IdToken doesn't match user id in request body." });
                }

                _context.Replies.Add(model);
                await _context.SaveChangesAsync();

                return Ok(model);
            }
            catch (Exception e)
            {
                return BadRequest(new { message = e.ToString() });
            }
        }

        // PUT: api/<RepliesController>/like/{userId}/{replyId}
        [HttpPut("like/{userId}/{replyId}")]
        public async Task<IActionResult> LikeReply(string userId, long replyId)
        {
            var like = await _context.Likes.FirstOrDefaultAsync(l => l.ReplyId == replyId && l.UserId == userId);

            if (like == null)
            {
                // If the like doesn't exist, create it
                like = new Like
                {
                    ReplyId = replyId,
                    UserId = userId
                };

                _context.Likes.Add(like);
                await _context.SaveChangesAsync();
                return Ok(new { message = "User successfully liked the post." });
            }
            else
            {
                // If the like exists, remove it
                _context.Likes.Remove(like);
                await _context.SaveChangesAsync();
                return Ok(new { message = "User successfully unliked the post." });
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
    }
}
