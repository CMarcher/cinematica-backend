namespace Cinematica.API.Models.User;

using System.ComponentModel.DataAnnotations;

public class FollowRequest
{
	[Required]
	public string UserId { get; set; }
	[Required]
	public string FollowerId { get; set; }
}