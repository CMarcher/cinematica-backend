﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using Cinematica.API.Data;
using Cinematica.API.Models.Display;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace Cinematica.API.Models.Database
{
    public partial class Post
    {
        public long PostId { get; set; }
        public string UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Body { get; set; }
        public string Image { get; set; }
        public bool isSpoiler { get; set; }

        public virtual User User { get; set; }
    }
}