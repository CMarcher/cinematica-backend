using System.ComponentModel.DataAnnotations;

namespace Cinematica.API.Models.User;

public class User
{
    // user_id is the primary key
    [Key]
    public string UserId { get; set; }

    // profile_picture is the URL of the user's profile picture
    public string ProfilePicture { get; set; }

    // cover_photo is the URL of the user's cover photo
    public string CoverPhoto { get; set; }
}