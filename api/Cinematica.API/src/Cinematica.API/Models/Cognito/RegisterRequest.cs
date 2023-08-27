namespace Cinematica.API.Models.Cognito;

using System.ComponentModel.DataAnnotations;

public class RegisterRequest
{
    [Required]
    public string? Username { get; set; }
    [Required]
    public string? Password { get; set; }
    [Required]
    public string? Email { get; set; }
}