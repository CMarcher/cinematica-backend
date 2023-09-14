using Cinematica.API.Models.Database;

namespace Cinematica.API.Models.Display;

public class PostDetails
{
    public Post Post { get; set; }
    public string UserName { get; set; }
    public string? ProfilePicture { get; set; }
    public int CommentsCount { get; set; }
    public int LikesCount { get; set; }
    public List<SimpleMovie> Movies { get; set; }
    public bool YouLike { get; set; }
}