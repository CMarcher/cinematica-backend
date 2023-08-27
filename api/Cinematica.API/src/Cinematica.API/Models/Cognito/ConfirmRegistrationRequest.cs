namespace Cinematica.API.Models.Cognito;

using System.ComponentModel.DataAnnotations;

public class ConfirmRegistrationRequest
{
    [Required]
    public string? ConfirmationCode { get; set; }
    [Required]
    public string? Username { get; set; }
}