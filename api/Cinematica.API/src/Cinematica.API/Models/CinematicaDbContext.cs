using Cinematica.API.Models;

using Microsoft.EntityFrameworkCore;

namespace Cinematica.API.Models;

public class CinematicaDbContext : DbContext
{
    public DbSet<User.User> Users { get; set; }
}