namespace Cinematica.API.Models.Cognito;

using System.ComponentModel.DataAnnotations;

public class ResetPassword
{
    public string ConfirmationCode { get; set; }

    [Required]
    public string Email { get; set; }
    [Required]
    public string Password { get; set; }
}