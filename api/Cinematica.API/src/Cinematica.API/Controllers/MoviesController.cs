using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.Search;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Cinematica.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MoviesController : ControllerBase
    {
        private readonly TMDbClient _tmdbClient;
        private readonly JsonSerializerOptions options;

        public MoviesController(TMDbClient tmdbClient)
        {
            _tmdbClient = tmdbClient;
            options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
        }

        // GET: api/<MoviesController>/{searchTerm}
        [HttpGet("search/{searchTerm}")]
        public string Get(string searchTerm)
        {
            SearchContainer<SearchMovie> results = _tmdbClient.SearchMovieAsync(searchTerm).Result;
            return JsonSerializer.Serialize(results, options);
        }

        // GET api/<MoviesController>/{id}
        [HttpGet("{id}")]
        public string Get(int id)
        {
            Movie movie = _tmdbClient.GetMovieAsync(id).Result;
            return JsonSerializer.Serialize(movie, options);
        }
    }
}
