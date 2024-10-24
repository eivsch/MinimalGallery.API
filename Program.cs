using Microsoft.AspNetCore.Mvc;
using MinimalGallery.API;
using MinimalGallery.API.Models;
using MinimalGallery.API.Storage;

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
    if (success)
    {
        return Results.Ok();
    }

    return Results.NoContent();
})
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status200OK);

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

// Endpoints - Files
// app.MapPost("/files/{username}/albums/{albumName}", (string username, string albumName) => {

// })

app.Run();