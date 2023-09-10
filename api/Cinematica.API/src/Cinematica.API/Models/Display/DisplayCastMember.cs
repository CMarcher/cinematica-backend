using System.ComponentModel.DataAnnotations;

namespace Cinematica.API.Models.Display;

public class DisplayCastMember
{
    [Required]
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Role { get; set; }
}