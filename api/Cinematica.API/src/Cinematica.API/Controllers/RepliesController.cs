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
    [Route("[controller]")]
    [ApiController]
    [Authorize]
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
        public async Task<IActionResult> CreateReply([FromBody] Reply model)
        {
            try
            {
                // checks if id token sub matches user id in request
                var valid = _helper.CheckTokenSub(HttpContext.Request.Headers["Authorization"].ToString(), model.UserId);
                if (!valid.Item1) return Unauthorized(new { message = valid.Item2 });

                _context.Replies.Add(model);
                await _context.SaveChangesAsync();

                return Ok(model);
            }
            catch (Exception e)
            {
                return BadRequest(ExceptionHander.HandleException(e));
            }
        }

        // PUT: api/<RepliesController>/like/{userId}/{replyId}
        [HttpPut("like/{userId}/{replyId}")]
        public async Task<IActionResult> LikeReply(string userId, long replyId)
        {
            // checks if id token sub matches user id in request
            var valid = _helper.CheckTokenSub(HttpContext.Request.Headers["Authorization"].ToString().Split(" ")[1], userId);
            if (!valid.Item1) return Unauthorized(new { message = valid.Item2 });

            try
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
            catch (Exception exception)
            {
                return BadRequest(ExceptionHander.HandleException(exception));
            }
        }

        // DELETE api/<RepliesController>/1
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReply(long id)
        {
            try
            {
                var reply = await _context.Replies.FindAsync(id);

                // checks if id token sub matches user id in request
                var valid = _helper.CheckTokenSub(HttpContext.Request.Headers["Authorization"].ToString().Split(" ")[1], reply.UserId);
                if (!valid.Item1) return Unauthorized(new { message = valid.Item2 });

                var likedReplies = await _context.Likes
                                            .Where(l => l.ReplyId == id)
                                            .ToListAsync();

                foreach (var like in likedReplies)
                {
                    _context.Likes.Remove(like);
                }

                _context.Replies.Remove(reply);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Successfully removed reply." });
            }
            catch (Exception e)
            {
                return BadRequest(ExceptionHander.HandleException(e));
            }
        }
    }
}
