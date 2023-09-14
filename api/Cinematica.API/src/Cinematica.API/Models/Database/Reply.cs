using Cinematica.API.Data;
using Cinematica.API.Models.Display;
using System;
using System.Collections.Generic;

namespace Cinematica.API.Models.Database
{
    public partial class Reply
    {
        public long? ReplyId { get; set; }
        public long? PostId { get; set; }
        public string UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Body { get; set; }

        public virtual Post Post { get; set; }
        public virtual User User { get; set; }

        public static ReplyDetails ConvertDetails(Reply reply, DataContext _context)
        {
            var user = _context.Users.FindAsync(reply.UserId).Result;

            return new ReplyDetails()
            {
                Reply = reply,
                UserName = user.UserName,
                ProfilePicture = user.ProfilePicture,
            };
        }
    }
}