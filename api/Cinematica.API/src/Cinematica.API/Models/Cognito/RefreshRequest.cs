namespace Cinematica.API.Models.Cognito;

using System.ComponentModel.DataAnnotations;

public class RefreshRequest
{
    [Required]
    public string Username { get; set; }
    [Required]
    public string RefreshToken { get; set; }
}