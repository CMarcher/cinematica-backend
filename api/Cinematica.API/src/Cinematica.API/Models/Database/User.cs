﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using Cinematica.API.Data;
using Cinematica.API.Models.Display;

namespace Cinematica.API.Models.Database
{
    public partial class User
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string ProfilePicture { get; set; }
        public string CoverPicture { get; set; }

        public virtual ICollection<Post> Posts { get; set; }
    }
}