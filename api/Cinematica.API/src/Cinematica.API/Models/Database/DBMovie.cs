using System;
using System.Collections.Generic;
using Cinematica.API.Data;
using Cinematica.API.Models.Display;
using TMDbLib.Objects.Movies;

namespace Cinematica.API.Models.Database
{
    public partial class DBMovie
    {
        public int MovieId { get; set; }
        public string? Title { get; set; }
        public DateOnly? ReleaseDate { get; set; }
        public string? Director { get; set; }
        public string? Poster { get; set; }
        public string? Banner { get; set; }
        public string? Language { get; set; }
        public string? RunningTime { get; set; }
        public string? Overview { get; set; }

        //Function to map TMDbLib.Movie to DBMovie
        public static DBMovie MapToDBMovie(Movie movie)
        {
            DBMovie dbMovie = new()
            {
                MovieId = movie.Id,
                Title = movie.Title,
                ReleaseDate = DateOnly.FromDateTime(movie.ReleaseDate.GetValueOrDefault()),
                Director = movie.Credits.Crew.FirstOrDefault(c => c.Job == "Director")?.Name,
                Poster = movie.PosterPath,
                Banner = movie.BackdropPath,
                Language = movie.OriginalLanguage,
                RunningTime = movie.Runtime.ToString(),
                Overview = movie.Overview
            };
            return dbMovie;
        }

        //Function to Map DBMovie to DisplayMovie
        public static DisplayMovie toDisplayMovie(DBMovie movie, DataContext _context)
        {
            var studioList = _context.MovieStudios
                .Where(ms => ms.MovieId == movie.MovieId)
                .Select(s=>s.Studio.StudioName)
                .ToList();

            var castList = _context.CastMembers
                .Where(cm => cm.MovieId == movie.MovieId)
                .Select(p => new DisplayCastMember()
                {
                    Id = p.PersonId,
                    Name = p.Person.PersonName,
                    Role = p.Role
                })
                .ToList();

            DisplayMovie displayMovie = new()
            {
                Id = movie.MovieId,
                Title = movie.Title,
                ReleaseYear = movie.ReleaseDate?.ToString("yyyy"),
                Poster = movie.Poster,
                Banner = movie.Banner,
                ReleaseDate = movie.ReleaseDate?.ToShortDateString(),
                Director = movie.Director,
                Genres = _context.MovieGenres.Where(m=>m.MovieId==movie.MovieId).Select(g=>g.Genre).ToList(),
                RunningTime = movie.RunningTime,
                Overview = movie.Overview,
                Language = movie.Language,
                Studios = studioList,
                Cast = castList
            };
            return displayMovie;
        }
    }
}