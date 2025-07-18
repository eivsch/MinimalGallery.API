using Microsoft.AspNetCore.Mvc;
using MinimalGallery.API;
using MinimalGallery.API.Models;
using MinimalGallery.API.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("https://0.0.0.0:5041", "http://0.0.0.0:5042");

builder.Services.AddCors();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
app.UseSwagger();
app.UseSwaggerUI();
// }

 app.UseCors(builder => builder
     .AllowAnyOrigin()
     .AllowAnyMethod()
     .AllowAnyHeader()); 

app.UseExceptionHandler(exHandler => exHandler.Run(async context => 
    {
        await Results.Problem().ExecuteAsync(context);
    })
);

// Misc
Globals.StoragePath = app.Configuration.GetValue<string>("IndexFilesStoragePath") ?? throw new Exception("'IndexFilesStoragePath' is a required config value.");

// Endpoints - Indices
app.MapPost("/users", (NewUserRequest r) => 
{
    bool userCreated = MinimalGallery.API.Storage.UserMetaHandler.CreateNewUser(r);
    if (!userCreated) return Results.Ok();

    return Results.Created();
})
.Produces(StatusCodes.Status201Created)
.Produces(StatusCodes.Status200OK);

app.MapGet("/users/{username}", (string username) => 
{
    UserCredentials? userCredentials = RequestHelper.GetUserCredentials(username); 
    return Results.Ok(userCredentials);
});

app.MapDelete("/users/{username}", (string username) => 
{
    bool deleted = MinimalGallery.API.Storage.UserMetaHandler.DeleteUserMeta(username);
    return deleted ? Results.NoContent() : Results.Ok();
})
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status200OK);

app.MapPost("/users/{userName}/albums", (string userName, NewAlbumRequest r) => 
{
    RequestHelper.CreateNewAlbum(userName, r.AlbumName);
    return Results.Created();
}).Produces(StatusCodes.Status201Created);

app.MapGet("/users/{username}/albums", (string username) => 
{
    UserMeta? data = MinimalGallery.API.Storage.UserMetaHandler.GetUserMeta(username);
    if (data == null) return null;
    foreach (UserAlbumMeta a in data.AlbumMeta)
    {
        int totalCount = AlbumIndexHandler.GetAlbumItemsCount(username, a.AlbumName);
        a.TotalCount = totalCount;
    }

    return data?.AlbumMeta;
});

app.MapGet("/users/{username}/albums/{albumName}", (string username, string albumName, int from = 0, int size = 32) => 
{
    int albumCount = AlbumIndexHandler.GetAlbumItemsCount(username, albumName);
    List<Media>? items = AlbumIndexHandler.GetAlbumItems(username, albumName, from, size);
    var result = new
    {
        TotalCount = albumCount,
        Items = items
    };
    
    return result;
});


app.MapDelete("/users/{username}/albums/{albumName}", (string username, string albumName) => 
{
    bool deleted = RequestHelper.DeleteAlbum(username, albumName);
    return deleted ? Results.NoContent() : Results.Ok();
})
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status200OK);

app.MapPost("/users/{userName}/albums/{albumName}/media-items",(string userName, string albumName, NewMediaRequest r) => 
{
    RequestHelper.AddNewMedia(userName, albumName, r);
    return Results.Created();
}).Produces(StatusCodes.Status201Created);

app.MapPatch("/users/{username}/albums/{albumName}/{mediaLocator}/likes", (string username, string albumName, string mediaLocator) => 
{
    bool success = RequestHelper.IncreaseLikedCount(username, albumName, mediaLocator);
    if (!success)
    {
        // TODO
    }

    return Results.NoContent();
})
.Produces(StatusCodes.Status204NoContent);

app.MapGet("/users/{userName}/albums/{albumName}/{mediaLocator}", (string userName, string albumName, string mediaLocator) => 
{
    // mediaLocator can be e.g. a name or id
    Media? m = MinimalGallery.API.Storage.AlbumIndexHandler.GetMedia(userName, albumName, mediaLocator);
    return Results.Ok(m);
});

app.MapDelete("/users/{username}/albums/{albumName}/{mediaLocator}", (string username, string albumName, string mediaLocator) => 
{
    bool deleted = RequestHelper.DeleteMedia(username, albumName, mediaLocator);
    return deleted ? Results.NoContent() : Results.Ok();
})
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status200OK);

app.MapPost("/users/{userName}/albums/{albumName}/{mediaLocator}/tags", (string userName, string albumName, string mediaLocator, NewTagRequest r) => 
{
    bool created = RequestHelper.AddTag(userName, albumName, mediaLocator, r);
    return created ? Results.Created() : Results.Ok();
})
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status201Created);

app.MapDelete("/users/{username}/albums/{albumName}/{mediaLocator}/tags/{tag}", (string username, string albumName, string mediaLocator, string tag) => {
    bool deleted = RequestHelper.DeleteTag(username, albumName, mediaLocator, tag);
    return deleted ? Results.NoContent() : Results.Ok();
})
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status204NoContent);

app.MapGet("/users/{username}/search", (string username, string? albums = null, string? tags = null, string? fileExtensions = null, string? mediaNameContains = null, int maxSize = 128, bool allTagsMustMatch = true, int hitsToSkip = 0) => 
{
    List<SearchHit>? result = RequestHelper.Search(username, albums, tags, fileExtensions, mediaNameContains, maxSize, allTagsMustMatch: allTagsMustMatch, hitsToSkip: hitsToSkip);
    return result == null ? Results.NoContent() : Results.Ok(result);
})
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status400BadRequest);

// Endpoints - Files
// app.MapPost("/files/{username}/albums/{albumName}", (string username, string albumName) => {

// })

app.MapPost("/users/{username}/saved-searches", (string username, SavedSearchMeta search) =>
{
    UserMetaHandler.AddSavedSearch(username, search);

    return Results.Created();
})
.Produces(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest);

app.MapGet("/users/{username}/saved-searches", (string username) =>
{
    var user = UserMetaHandler.GetUserMeta(username);
    if (user == null) return Results.NotFound();

    return Results.Ok(user.SavedSearches ?? new List<SavedSearchMeta>());
})
.Produces<List<SavedSearchMeta>>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.MapDelete("/users/{username}/saved-searches/{searchName}", (string username, string searchName) =>
{
    var user = UserMetaHandler.GetUserMeta(username);
    if (user == null) return Results.NotFound();

    if (user.SavedSearches == null || !user.SavedSearches.Any(s => s.SearchName == searchName))
        return Results.NotFound();

    user.SavedSearches.RemoveAll(s => s.SearchName == searchName);
    UserMetaHandler.WriteUser(user);

    return Results.NoContent();
})
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound);

app.Run();