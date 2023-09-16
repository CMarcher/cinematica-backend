namespace Cinematica.API.Models.User;

using System.ComponentModel.DataAnnotations;

public class MovieRequest
{
    [Required]
    public string UserId { get; set; }
    [Required]
    public int MovieId { get; set; }
}