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
    bool userCreated = UserHandler.CreateNewUser(r.UserName);
    if (!userCreated) return Results.Ok();

    return Results.Created();
})
.Produces(StatusCodes.Status201Created)
.Produces(StatusCodes.Status200OK);

app.MapGet("/users/{username}", (string username) => 
{
    UserMeta? userMeta = UserHandler.GetUserMeta(username); 
    return Results.Ok(userMeta);
});

app.MapDelete("/users/{username}", (string username) => 
{
    bool deleted = UserHandler.DeleteUser(username);
    return deleted ? Results.NoContent() : Results.Ok();
})
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status200OK);

app.MapPost("/users/{userName}/albums", (string userName, NewAlbumRequest r) => 
{
    FileStorageHandler.CreateNewAlbum(userName, r.AlbumName);
    return Results.Created();
}).Produces(StatusCodes.Status201Created);

app.MapDelete("/users/{username}/albums/{albumName}", (string username, string albumName) => 
{
    bool deleted = FileStorageHandler.DeleteAlbum(username, albumName);
    return deleted ? Results.NoContent() : Results.Ok();
})
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status200OK);

app.MapPost("/users/{userName}/albums/{albumName}/media-items",(string userName, string albumName, NewMediaRequest r) => 
{
    FileStorageHandler.AddNewMedia(userName, albumName, r);
    return Results.Created();
}).Produces(StatusCodes.Status201Created);

app.MapGet("/users/{userName}/albums/{albumName}/{mediaLocator}", (string userName, string albumName, string mediaLocator) => 
{
    // mediaLocator can be e.g. a name or id
    Media? m = FileStorageHandler.GetMedia(userName, albumName, mediaLocator);
    return Results.Ok(m);
});

app.MapDelete("/users/{username}/albums/{albumName}/{mediaLocator}", (string username, string albumName, string mediaLocator) => 
{
    bool deleted = FileStorageHandler.DeleteMedia(username, albumName, mediaLocator);
    return deleted ? Results.NoContent() : Results.Ok();
})
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status200OK);

app.MapPost("/users/{userName}/albums/{albumName}/{mediaLocator}/tags", (string userName, string albumName, string mediaLocator, NewTagRequest r) => 
{
    bool created = FileStorageHandler.AddTag(userName, albumName, mediaLocator, r);
    return created ? Results.Created() : Results.Ok();
})
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status201Created);

app.MapDelete("/users/{username}/albums/{albumName}/{mediaLocator}/tags", (string username, string albumName, string mediaLocator, DeleteTagRequest r) => 
{
    bool deleted = FileStorageHandler.DeleteTag(username, albumName, mediaLocator, r);
    return deleted ? Results.NoContent() : Results.Ok();
})
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status200OK);

app.Run();