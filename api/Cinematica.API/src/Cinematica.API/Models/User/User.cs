using System.ComponentModel.DataAnnotations;

namespace Cinematica.API.Models.User;

public class User
{
    // user_id is the primary key
    [Key]
    public string UserId { get; set; }

    // UserName is the displayed name for the user
    [Required]
    public string UserName { get; set; }

    [Required]
    public string Email { get; set; }

    [Required]
    public bool Verified { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    // profile_picture is the URL of the user's profile picture
    public string ProfilePicture { get; set; }

    // cover_photo is the URL of the user's cover photo
    public string CoverPhoto { get; set; }

    // FollowerCount is how many people are following this account
    public int FollowerCount { get; set; }

    // FollowingCount is how many accounts this user is following
    public int FollowingCount { get; set; }
}