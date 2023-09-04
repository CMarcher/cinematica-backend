using Cinematica.API.Models.TMDb;
using Microsoft.AspNetCore.Mvc;
using TMDbLib.Client;

namespace Cinematica.API.Controllers;

public class MoviesController : ControllerBase
{
    private readonly TMDbClient _tmdbClient;

    public MoviesController(TMDbClient tmdbClient)
    {
        _tmdbClient = tmdbClient;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(string query)
    {
        try
        {
            var searchResults = await _tmdbClient.SearchMovieAsync(query);

            // Map the search results from TMDbLib models to your MovieSearchResult model
            var movieSearchResults = searchResults.Results.Select(result => new MovieSearchResults
            {
                Id = result.Id,
                Title = result.Title,
                Overview = result.Overview,
                ReleaseDate = (DateTime)result.ReleaseDate
                // Map other properties as needed
            }).ToList();

            // Create a JsonResult with the search results
            var jsonResult = new JsonResult(movieSearchResults);

            return jsonResult; // Return JSON data
        }
        catch (Exception ex)
        {
            // Handle errors
            return BadRequest($"Error: {ex.Message}");
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var movie = await _tmdbClient.GetMovieAsync(id);

            // Map the movie details from TMDbLib models to your MovieDetails model
            var movieDetails = new MovieDetails
            {
                Id = movie.Id,
                Title = movie.Title,
                Overview = movie.Overview,
                Genres = movie.Genres.Select(genre => genre.Name).ToList(),
                Cast = movie.Credits.Cast.Select(actor => new Actor
                {
                    Id = actor.Id,
                    Name = actor.Name
                    // Map other actor-related properties as needed
                }).ToList()
                // Map other properties as needed
            };

            // Create a JsonResult with the search results
            var jsonResult = new JsonResult(movieDetails);

            return jsonResult; // Return JSON data
        }
        catch (Exception ex)
        {
            // Handle errors
            return BadRequest($"Error: {ex.Message}");
        }
    }

}
