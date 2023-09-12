namespace Cinematica.API.Models.User;

using System.ComponentModel.DataAnnotations;

public class SetPictureRequest
{
    [Required]
    public string UserId { get; set; }
    [Required]
    public IFormFile File { get; set; }
}