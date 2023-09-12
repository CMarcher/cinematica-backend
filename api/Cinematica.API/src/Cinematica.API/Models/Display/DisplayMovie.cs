namespace Cinematica.API.Models.Display;

public class DisplayMovie : SimpleMovie
{
    public string? Poster { get; set; }
    public string? Banner { get; set; }
    public string? ReleaseDate { get; set; }
    public string? Director { get; set; }
    public List<string>? Genres { get; set; }
    public string? RunningTime { get; set; }
    public string? Overview { get; set; }
    public string? Language { get; set; }
    public List<string>? Studios { get; set; }
    public List<DisplayCastMember>? Cast { get; set; }
}