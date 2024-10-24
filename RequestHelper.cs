using MinimalGallery.API.Models;
using MinimalGallery.API.Storage;

namespace MinimalGallery.API;

static class RequestHelper
{
    public static UserCredentials? GetUserCredentials(string username)
    {
        UserMeta? data = UserMetaHandler.GetUserMeta(username);
        if (data == null) return null;

        return new UserCredentials
        {
            Username = data.Username,
            Password = data.Password
        };
    }

    public static void CreateNewAlbum(string username, string albumName)
    {
        AlbumIndexHandler.CreateIndex(username, albumName);
        UserMetaHandler.InitializeAlbumMeta(username, albumName);
    }

    public static bool DeleteAlbum(string username, string albumName)
    {
        bool deleted = AlbumIndexHandler.DeleteIndex(username, albumName);
        UserMetaHandler.DeleteAlbumMeta(username, albumName);

        return deleted;
    }

    public static void AddNewMedia(string username, string albumName, NewMediaRequest r)
    {
        var mediaData = new Media 
        {
            Id = Guid.NewGuid().ToString(),
            Name = r.Name,
            Size = r.Size,
            Created = DateTimeOffset.Now
        };

        AlbumIndexHandler.AddMedia(username, albumName, mediaData);
    }

    public static bool DeleteMedia(string username, string albumName, string searchTerm)
    {
        (Media? media, int? i) = AlbumIndexHandler.FindMediaChunk(username, albumName, searchTerm);
        if (media == null || i == null) return false;

        AlbumIndexHandler.DeleteMediaChunk(username, albumName, i.Value);
        UserMetaHandler.HandleMediaDeletion(username, albumName, media);

        return true;
    }

    public static bool AddTag(string username, string albumName, string searchTerm, NewTagRequest r)
    {
        (Media? media, int? i) = AlbumIndexHandler.FindMediaChunk(username, albumName, searchTerm);
        if (media == null || i == null) return false;

        if (media.Tags.Any(a => a.TagName == r.TagName)) return false;
        Tag t = new() { TagName = r.TagName, Created = DateTime.UtcNow };
        media.Tags.Add(t);
        
        AlbumIndexHandler.WriteMediaChunk(username, albumName, i.Value, media);
        UserMetaHandler.AddTagMeta(username, albumName, t);

        return true;
    }

    public static bool DeleteTag(string username, string albumName, string mediaLocator, string tag)
    {
        (Media? media, int? i) = AlbumIndexHandler.FindMediaChunk(username, albumName, mediaLocator);
        if (media == null || i == null) return false;
        
        Tag? t = media.Tags.FirstOrDefault(f => f.TagName == tag);
        if (t == null) return false;
        else media.Tags.Remove(t);
        
        AlbumIndexHandler.WriteMediaChunk(username, albumName, i.Value, media);
        UserMetaHandler.HandleTagDeletion(username, albumName, [t]);

        return true;
    }

    public static bool IncreaseLikedCount(string username, string albumName, string searchTerm)
    {
        (Media? media, int? i) = AlbumIndexHandler.FindMediaChunk(username, albumName, searchTerm);
        if (media == null || i == null) return false;

        bool isFirstLike = media.Likes == 0;
        media.Likes++;
        AlbumIndexHandler.WriteMediaChunk(username, albumName, i.Value, media);
        
        UserMetaHandler.IncreaseLikeCount(username, albumName, isFirstLike);

        return true;
    }
}