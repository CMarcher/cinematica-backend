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

    // Function that takes a TMDbLib.Objects.Movies.DisplayMovie object as a parameter and returns a DisplayMovie object
    public static DisplayMovie MapToMovie(TMDbLib.Objects.Movies.Movie tmdbMovie)
    {
        // Create a new DisplayMovie object and assign the properties from the tmdbMovie object
        DisplayMovie displayMovie = new()
        {
            Id = tmdbMovie.Id,
            Title = tmdbMovie.Title,
            ReleaseYear = tmdbMovie.ReleaseDate?.ToString("yyyy"),
            Poster = "http://image.tmdb.org/t/p/w500" + tmdbMovie.PosterPath,
            Banner = "http://image.tmdb.org/t/p/w500" + tmdbMovie.BackdropPath,
            ReleaseDate = tmdbMovie.ReleaseDate?.ToShortDateString(),
            Director = tmdbMovie.Credits.Crew.FirstOrDefault(c => c.Job == "Director")?.Name,
            Genres = tmdbMovie.Genres.Select(g => g.Name).ToList(),
            RunningTime = tmdbMovie.Runtime?.ToString(),
            Overview = tmdbMovie.Overview,
            Language = tmdbMovie.OriginalLanguage,
            Studios = tmdbMovie.ProductionCompanies.Select(p => p.Name).ToList(),
            Cast = tmdbMovie.Credits.Cast.Select(c => new DisplayCastMember
                {
                    Id = c.Id,
                    Name = c.Name,
                    Role = c.Character
                }).ToList(),
        };

        return displayMovie;
    }
}