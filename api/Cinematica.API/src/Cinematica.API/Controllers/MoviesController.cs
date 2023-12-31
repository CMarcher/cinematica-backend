﻿using Cinematica.API.Models;
using Cinematica.API.Models.Database;
using Cinematica.API.Models.Display;
using Cinematica.API.Data;
using Cinematica.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.Search;
using static System.Net.WebRequestMethods;

namespace Cinematica.API.Controllers;

[Route("[controller]")]
[ApiController]
public class MoviesController : ControllerBase
{
    private readonly TMDbClient _tmdbClient;
    private readonly DataContext _context;
    private readonly IHelperService _helper;
    private const string TmDbPath = "http://image.tmdb.org/t/p/w500";
    private readonly string _movieFiles;
    private readonly ImageSettings _imageSettings;

    public MoviesController(DataContext context, TMDbClient tmdbClient, IHelperService helperService, ImageSettings imageSettings)
    {
        _context = context;
        _tmdbClient = tmdbClient;
        _helper = helperService;
        _imageSettings = imageSettings;
        _movieFiles = "movies";
    }

    // GET: api/<MoviesController>/{searchTerm}
    [HttpGet("search/{searchTerm}")]
    public async Task<IActionResult> Get(string searchTerm)
    {
        SearchContainer<SearchMovie> results = await _tmdbClient.SearchMovieAsync(searchTerm);
        if (results == null)
        {
            return NotFound(); // Return a 404 Not Found response
        }
        //Pair down results into SimpleMovie type and return.
        return Ok(SimpleMovie.TMDbToSimpleMovies(results));
    }

    // GET api/<MoviesController>/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        //Check if already in database, if it is then return data from cache and database, else fetch from TMDb
        
        var checkMovie = await _context.Movies.FirstOrDefaultAsync(m => m.MovieId == id);
        if (checkMovie == null)
        {
            //Fetch from TMDb
            var movie = await _tmdbClient.GetMovieAsync(id, MovieMethods.Credits);
            if (movie == null)
            {
                return NotFound(); // Return a 404 Not Found response
            }

            // Add movie to database
            var newMovie = DBMovie.MapToDBMovie(movie);

            // Add images to file cache and update newMovie with new file names.
            newMovie.Poster = await _helper.DownloadFile(TmDbPath + movie.PosterPath, _movieFiles);
            newMovie.Banner = await _helper.DownloadFile(TmDbPath + movie.BackdropPath, _movieFiles);
            
            _context.Movies.Add(newMovie);
            //Add new Persons if not already in DB
            var cast = movie.Credits.Cast
                .Where(c => !_context.Persons.Any(p => p.PersonId == c.Id))
                .Select(c => new Person()
                {
                    PersonId = c.Id,
                    PersonName = c.Name,
                })
                .ToList();
            _context.Persons.AddRange(cast);

            //Add CastMembers to Movie
            var castList = movie.Credits.Cast.Select(c => new CastMember()
            {
                PersonId = c.Id,
                MovieId = movie.Id,
                Role = c.Character
            }).ToList();
            _context.CastMembers.AddRange(castList);

            //Add Genres if they don't already exist in DB, then Add to MovieGenres
            var genres = movie.Genres
                .Select(g => new MovieGenres()
                {
                    MovieId = movie.Id,
                    Genre = g.Name
                })
                .ToList();
            _context.MovieGenres.AddRange(genres);

            //Add Studios if they don't already exist in Db, then Add to MovieStudios
            var studios = movie.ProductionCompanies
                .Where(c=> !_context.Studios.Any(s => s.StudioId==c.Id))
                .Select(c => new Studio()
                {
                    StudioId = c.Id,
                    StudioName = c.Name
                }).ToList();
            _context.Studios.AddRange(studios);

            var studioList = movie.ProductionCompanies.Select(s=> new MovieStudios()
            {
                MovieId = movie.Id,
                StudioId = s.Id
            }).ToList();
            _context.MovieStudios.AddRange(studioList);

            await _context.SaveChangesAsync();

            // Return DisplayMovie
            return Ok(DBMovie.ToDisplayMovie(newMovie, _context, _imageSettings));
        }

        return Ok(DBMovie.ToDisplayMovie(checkMovie, _context, _imageSettings));
    }

    [HttpGet("withPosts/{searchTerm}")]
    public async Task<IActionResult> GetMoviesWithPosts(string searchTerm)
    {
        // Convert searchTerm to lower case
        var lowerCaseSearchTerm = searchTerm.ToLower();

        // Get the movies that have posts about them and match the search term
        var movies = await _context.MovieSelections
            .Where(m => m.Movie.Title.ToLower().Contains(lowerCaseSearchTerm)) // Filter by movie title
            .Select(m => m.Movie) // Select the associated movies
            .Distinct() // Remove duplicates
            .ToListAsync();

        if (movies == null)
        {
            return NotFound();
        }

        // Convert DBMovie to SimpleMovie
        var simpleMovies = movies.Select(DBMovie.DbMovieToSimpleMovie).ToList();

        // Return the list of SimpleMovie objects
        return Ok(simpleMovies);
    }

}

