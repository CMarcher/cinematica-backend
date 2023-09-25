namespace Cinematica.API.Models.User;

using System.ComponentModel.DataAnnotations;
using Cinematica.API.Models.Display;

public class SetPictureRequest
{
    [Required]
    public string UserId { get; set; }
    [Required]
    public ImageModel Image { get; set; }
}