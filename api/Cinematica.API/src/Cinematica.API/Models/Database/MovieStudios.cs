﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace Cinematica.API.Models.Database
{
    public partial class MovieStudios
    {
        public int MovieId { get; set; }
        public int StudioId { get; set; }

        public virtual DBMovie Movie { get; set; }
        public virtual Studio Studio { get; set; }
    }
}