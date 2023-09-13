using Cinematica.API.Models.Database;

namespace Cinematica.API.Models.Display;

public class PostDetails
{
    public Post Post { get; set; }
    public int CommentsCount { get; set; }
    public int LikesCount { get; set; }
    public List<SimpleMovie> Movies { get; set; }
    public bool youLike { get; set; }
}