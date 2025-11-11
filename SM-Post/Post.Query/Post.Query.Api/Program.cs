using Microsoft.EntityFrameworkCore;
using Post.Query.Domain.Repositories;
using Post.Query.Infrastructure.DataAccess;
using Post.Query.Infrastructure.Handlers;
using Post.Query.Infrastructure.Repositories;


var builder = WebApplication.CreateBuilder(args);

//Add services to the container

Action<DbContextOptionsBuilder> configureDbContext = (o => o.UseLazyLoadingProxies().UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));
builder.Services.AddDbContext<DatabaseContext>(configureDbContext);
builder.Services.AddSingleton<DatabaseContextFactory>(new DatabaseContextFactory(configureDbContext));

// Create database and tables from code

var dataContext = builder.Services.BuildServiceProvider().GetRequiredService<DatabaseContext>();
dataContext.Database.EnsureCreated();

builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IEventHandler, Post.Query.Infrastructure.Handlers.EventHandler>();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
