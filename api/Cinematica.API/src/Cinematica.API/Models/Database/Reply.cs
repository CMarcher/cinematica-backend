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
    }
}