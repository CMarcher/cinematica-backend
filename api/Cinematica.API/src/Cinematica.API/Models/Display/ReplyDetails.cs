using Cinematica.API.Models.Database;

namespace Cinematica.API.Models.Display;

public class ReplyDetails
{
    public Reply Reply { get; set; }
    public int LikesCount { get; set; }
    public bool YouLike { get; set; }
}