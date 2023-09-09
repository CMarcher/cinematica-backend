using System.ComponentModel.DataAnnotations;

namespace Cinematica.API.Models.Movie;

public class CastMember
{
    [Required]
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Role { get; set; }
}