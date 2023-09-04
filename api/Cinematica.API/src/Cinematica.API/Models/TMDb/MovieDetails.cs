namespace Cinematica.API.Models.TMDb
{
    public class MovieDetails
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Overview { get; set; }
        public List<string>? Genres { get; set; }
        public List<Actor>? Cast { get; set; }

    }
}
