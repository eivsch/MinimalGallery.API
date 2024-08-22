namespace MinimalGallery.API;

using MinimalGallery.API.Models;
using MinimalGallery.API.Storage;

static class RequestHelper
{
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
        (Media? media, int? i) = AlbumIndexHandler.ReadMediaChunk(username, albumName, searchTerm);
        if (media == null || i == null) return false;

        AlbumIndexHandler.DeleteMediaChunk(username, albumName, i.Value);
        UserMetaHandler.DeleteTagsMeta(username, albumName, media.Tags);

        return true;
    }

    public static bool AddTag(string username, string albumName, string searchTerm, NewTagRequest r)
    {
        (Media? media, int? i) = AlbumIndexHandler.ReadMediaChunk(username, albumName, searchTerm);
        if (media == null || i == null) return false;

        if (media.Tags.Any(a => a.TagName == r.TagName)) return false;
        Tag t = new() { TagName = r.TagName, Created = DateTime.UtcNow };
        media.Tags.Add(t);
        
        AlbumIndexHandler.WriteMediaChunk(username, albumName, i.Value, media);
        UserMetaHandler.AddTagMeta(username, albumName, t);

        return true;
    }

    public static bool DeleteTag(string username, string albumName, string mediaLocator, DeleteTagRequest r)
    {
        (Media? media, int? i) = AlbumIndexHandler.ReadMediaChunk(username, albumName, mediaLocator);
        if (media == null || i == null) return false;
        
        Tag? t = media.Tags.FirstOrDefault(f => f.TagName == r.TagName);
        if (t == null) return false;
        else media.Tags.Remove(t);
        
        AlbumIndexHandler.WriteMediaChunk(username, albumName, i.Value, media);
        UserMetaHandler.DeleteTagsMeta(username, albumName, [t]);

        return true;
    }
}