using Cinematica.API.Models.Movie;
using Microsoft.AspNetCore.Mvc;
using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.Search;
using Movie = Cinematica.API.Models.Movie.Movie;

namespace Cinematica.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MoviesController : ControllerBase
    {
        private readonly TMDbClient _tmdbClient;

        public MoviesController(TMDbClient tmdbClient)
        {
            _tmdbClient = tmdbClient;
        }

        // GET: api/<MoviesController>/{searchTerm}
        [HttpGet("search/{searchTerm}")]
        public IActionResult Get(string searchTerm)
        {
            SearchContainer<SearchMovie> results = _tmdbClient.SearchMovieAsync(searchTerm).Result;
            if (results == null)
            {
                return NotFound(); // Return a 404 Not Found response
            }
            //Pair down results into SimpleMovie type and return.
            return Ok(SimpleMovie.MapToSimpleMovies(results));
        }

        // GET api/<MoviesController>/{id}
        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            //Check if already in database, if it is then return data from cache and database, else fetch from TMDb

            //Fetch from TMDb
            TMDbLib.Objects.Movies.Movie movie = _tmdbClient.GetMovieAsync(id, MovieMethods.Credits).Result;
            if (movie == null)
            {
                return NotFound(); // Return a 404 Not Found response
            }
            // Add to database and Images to Cache

            // Return Movie
            return Ok(Movie.MapToMovie(movie));
        }
    }
}
