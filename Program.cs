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
FileStorageHandler.StoragePath = app.Configuration.GetValue<string>("IndexFilesStoragePath") ?? throw new Exception("'IndexFilesStoragePath' is a required config value.");

// Endpoints
app.MapPost("/users", (NewUserRequest r) => 
{
    bool userCreated = FileStorageHandler.CreateNewUser(r.UserName);
    if (!userCreated) return Results.Ok();

    return Results.Created();
})
.Produces(StatusCodes.Status201Created)
.Produces(StatusCodes.Status200OK);

app.MapPost("/users/{userName}/albums", (string userName, NewAlbumRequest r) => 
{
    FileStorageHandler.CreateNewAlbum(userName, r.AlbumName);
    return Results.Created();
}).Produces(StatusCodes.Status201Created);

app.MapPost("/users/{userName}/albums/{albumName}/media-items",(string userName, string albumName, NewMediaRequest r) => 
{
    FileStorageHandler.AddNewMedia(userName, albumName, r);
    return Results.Created();
}).Produces(StatusCodes.Status201Created);

app.MapGet("/users/{userName}/albums/{albumName}/{mediaLocator}", (string userName, string albumName, string mediaLocator) => 
{
    // searchTerm can be e.g. a name or id
    Media? m = FileStorageHandler.GetMedia(userName, albumName, mediaLocator);
    return Results.Ok(m);
});

app.MapPost("/users/{userName}/albums/{albumName}/{mediaName}/tags", (string userName, string albumName, string mediaName, NewTagRequest r) => 
{
    FileStorageHandler.AddTag(userName, albumName, mediaName, r);
    return Results.Created();
}).Produces(StatusCodes.Status201Created);

// app.MapGet("/search/users/{username}/tags/{tagName}")

app.Run();