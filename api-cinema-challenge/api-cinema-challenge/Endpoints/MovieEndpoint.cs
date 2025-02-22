﻿using api_cinema_challenge.Repository;
using api_cinema_challenge.Models.Domain.Entities.MoviesAndScreenings;
using api_cinema_challenge.Models.DTO.Entities.MoviesAndScreenings;
using api_cinema_challenge.Models.DTO;
using api_cinema_challenge.Models.Domain.Entities.CinemaInfrastructure;
using Microsoft.AspNetCore.Mvc;
using api_cinema_challenge.Repository.Specific;

namespace api_cinema_challenge.Endpoints
{
    public static class MovieEndpoint
    {
        public static void ConfigureMovieEndpoint(this WebApplication app)
        {
            var group = app.MapGroup("movies");
            group.MapGet("/", GetAll);
            group.MapPost("/", CreateMovie);
            group.MapPut("/{id}", Update);
            group.MapDelete("/{id}", Delete);
            group.MapPost("/{id}/screenings", CreateScreeningByMovieId);
            group.MapGet("/{id}/screenings", GetScreeningsByMovieId);
        }

        [ProducesResponseType(StatusCodes.Status200OK)]
        private static async Task<IResult> GetAll(IRepository<Movie> movieRepository)
        {
            IEnumerable<Movie> results = await movieRepository.GetAll();
            List<MovieOutputDTO> resultDTOs = new List<MovieOutputDTO>();
            foreach (Movie movie in results)
            {
                resultDTOs.Add(new MovieOutputDTO(movie));
            }
            return TypedResults.Ok(new Payload<List<MovieOutputDTO>>(resultDTOs));
        }

        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        private static async Task<IResult> Update(IRepository<Movie> movieRepository, int id, MoviePutDTO input)
        {
            Movie? movie = await movieRepository.GetById(id);
            if (movie == null) return TypedResults.NotFound($"Could not find any movie with id={id}.");
            movie.Title = input.Title;
            movie.Rating = input.Rating;
            movie.Description = input.Description;
            movie.RuntimeMins = input.RuntimeMins;
            movie.UpdatedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
            MovieOutputDTO resultDTO = new MovieOutputDTO(movie);
            return TypedResults.Created($"/{movie.Id}", new Payload<MovieOutputDTO>(resultDTO));
        }

        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        private static async Task<IResult> Delete(IRepository<Movie> movieRepository, int id)
        {
            Movie? result = await movieRepository.DeleteById(id);
            if (result == null) return TypedResults.NotFound($"Could not find any movie with id={id}.");
            MovieOutputDTO resultDTO = new MovieOutputDTO(result);
            return TypedResults.Ok(new Payload<MovieOutputDTO>(resultDTO));
        }

        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        private static async Task<IResult> CreateMovie(
            [FromServices] IRepository<Movie> movieRepository,
            [FromServices] IRepository<Screening> screeningRepository,
            [FromServices] IRepository<Auditorium> auditoriumRepository,
            [FromServices] IJunctionRepository<ScreeningSeat> screeningSeatRepository,
            [FromBody] MoviePostDTO input)
        {
            foreach (ScreeningInputDTO screening in input.Screenings)
            {

                Auditorium? auditorium = await auditoriumRepository.GetById(screening.ScreenNumber);
                if (auditorium == null) return TypedResults.NotFound(($"No screen with id = {screening.ScreenNumber}"));
            }
            Movie newMovie = new Movie()
            {
                Title = input.Title,
                Rating = input.Rating,
                Description = input.Description,
                RuntimeMins = input.RuntimeMins,
            };
            Movie movieResult = await movieRepository.Insert(newMovie);
            List<ScreeningOutputDTO> insertedScreenings = new List<ScreeningOutputDTO>();
            if (input.Screenings.Count > 0)
            {
                foreach (var screening in  input.Screenings)
                {
                    insertedScreenings.Add(await Services.CreateScreening(screeningRepository, auditoriumRepository, screeningSeatRepository, movieResult.Id, screening));
                }
            }
            MovieWithScreeningsOutputDTO results = new MovieWithScreeningsOutputDTO(movieResult, insertedScreenings);
            return TypedResults.Created($"/{movieResult.Id}", new Payload<MovieWithScreeningsOutputDTO>(results));
        }

        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        private static async Task<IResult> GetScreeningsByMovieId(IRepository<Movie> movieRepository, IScreeningRepository screeningEndpoint, int id)
        {
            Movie? movie = await movieRepository.GetById(id);
            if (movie == null) return TypedResults.BadRequest($"No movies with id={id}");
            IEnumerable<Screening> results = await screeningEndpoint.GetAllByMovieId(id);
            List<ScreeningOutputDTO> resultDTOs = new List<ScreeningOutputDTO>();
            foreach (Screening output in results)
            {
                int screenNumber = output.ScreeningSeats.First().Seat.AuditoriumId;
                int capacity = output.ScreeningSeats.Count();
                resultDTOs.Add(new ScreeningOutputDTO(output, screenNumber, capacity));
            }
            return TypedResults.Ok(new Payload<IEnumerable<ScreeningOutputDTO>>(resultDTOs));
        }

        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        private static async Task<IResult> CreateScreeningByMovieId(
            [FromServices] IRepository<Movie> movieRepository,
            [FromServices] IRepository<Screening> screeningRepository,
            [FromServices] IRepository<Auditorium> auditoriumRepository,
            [FromServices] IJunctionRepository<ScreeningSeat> screeningSeatRepository,
            [FromBody] ScreeningInputDTO input,
            [FromRoute] int id)
        {
            Movie? movie = await movieRepository.GetById(id);
            if (movie == null) return TypedResults.BadRequest($"No movies with id={id}");
            ScreeningOutputDTO resultDto = await Services.CreateScreening(screeningRepository, auditoriumRepository, screeningSeatRepository, movie.Id, input);
            return TypedResults.Created($"/{movie.Id}", new Payload<ScreeningOutputDTO>(resultDto));
        }
    }
}
