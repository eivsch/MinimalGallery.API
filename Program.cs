using Microsoft.AspNetCore.Mvc;
using MinimalGallery.API;
using MinimalGallery.API.Models;

var builder = WebApplication.CreateBuilder(args);

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
app.UseExceptionHandler(exHandler => exHandler.Run(async context => 
    {
        await Results.Problem().ExecuteAsync(context);
    })
);

// Misc
string storagePath = app.Configuration.GetValue<string>("IndexFilesStoragePath") ?? throw new Exception("'IndexFilesStoragePath' is a required config value.");


var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

// Endpoints
app.MapPost("/users", (NewUserRequest r) => 
{
    bool userCreated = FileStorageHandler.CreateNewUser(storagePath, r.UserName);
    if (!userCreated) return Results.Ok();

    return Results.Created();
})
.Produces(StatusCodes.Status201Created)
.Produces(StatusCodes.Status200OK);

app.MapPost("/users/{userName}/albums", (string userName, NewAlbumRequest r) => 
{
    FileStorageHandler.CreateNewAlbum(storagePath, userName, r.AlbumName);
    return Results.Created();
})
.Produces(StatusCodes.Status201Created);

app.MapPost("/users/{userName}/albums/{albumName}",(string userName, string albumName, NewMediaRequest r) => 
{
    FileStorageHandler.AddNewMedia(storagePath, userName, albumName, r);
    return Results.Created();
})
.Produces(StatusCodes.Status201Created);

app.MapGet("/users/{userName}/albums/{albumName}/{searchTerm}", (string userName, string albumName, string searchTerm) => 
{
    // searchTerm can be e.g. a name or id
    Media? m = FileStorageHandler.GetMedia(storagePath, userName, albumName, searchTerm);
    return Results.Ok(m);
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
