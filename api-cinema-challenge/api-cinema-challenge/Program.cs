using api_cinema_challenge.Data;
using api_cinema_challenge.Endpoints;
using api_cinema_challenge.Models.Domain.Entities.CinemaInfrastructure;
using api_cinema_challenge.Models.Domain.Entities.MoviesAndScreenings;
using api_cinema_challenge.Models.Domain.Entities.SalesAndTickets;
using api_cinema_challenge.Repository;
using api_cinema_challenge.Repository.Generic;
using api_cinema_challenge.Repository.Specific;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<DatabaseContext>();
builder.Services.AddScoped<IRepository<Customer>, Repository<Customer>>();
builder.Services.AddScoped<IRepository<Movie>, Repository<Movie>>();
builder.Services.AddScoped<IRepository<Screening>, Repository<Screening>>();
builder.Services.AddScoped<IRepository<Auditorium>, Repository<Auditorium>>();
builder.Services.AddScoped<IRepository<Ticket>, Repository<Ticket>>();
builder.Services.AddScoped<IRepository<Movie>, Repository<Movie>>();
builder.Services.AddScoped<IJunctionRepository<ScreeningSeat>, JunctionRepository<ScreeningSeat>>();
builder.Services.AddScoped<IScreeningRepository, ScreeningRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.ConfigureCustomerEndpoint();
app.ConfigureMovieEndpoint();
app.Run();
