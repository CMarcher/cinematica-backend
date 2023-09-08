using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;

namespace Cinematica.API.Models.Movie;

public class SimpleMovie
{
    [Required]
    public int Id { get; set; }
    [Required]
    public string? Title { get; set; }
    public string? ReleaseYear { get; set; }

    public static List<SimpleMovie> MapToSimpleMovies(SearchContainer<SearchMovie> searchContainer)
    {
        var simpleMovies = searchContainer.Results
            .Where(movie => !movie.Adult)
            .Select(movie => new SimpleMovie
            {
                Id = movie.Id,
                Title = movie.Title,
                ReleaseYear = movie.ReleaseDate?.ToString("yyyy"),
            })
            .ToList();

        return simpleMovies;
    }
}

