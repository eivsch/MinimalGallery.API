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

    public static List<SearchHit>? Search(string username, string? albumsStr, string? tagsStr, string? fileExtensionsStr, string? mediaNameContains, int maxSize, bool allTagsMustMatch = true)
    {
        string[] albumsArray = [];
        if (albumsStr is not null) albumsArray = albumsStr.Split(",");
        string[] tagsArray = [];
        if (tagsStr is not null) tagsArray = tagsStr.Split(',');
        string[] fileExtensionsArray = [];
        if (fileExtensionsStr is not null) fileExtensionsArray = fileExtensionsStr.Split(',');

        List<SearchHit> hits = [];
        UserMeta? userMetaData = UserMetaHandler.GetUserMeta(username);
        if (userMetaData == null) return null;

        List<UserAlbumMeta> albumsToSearch = FilterListOfAlbums(userMetaData.AlbumMeta, albumsArray, tagsArray, allTagsMustMatch);
        foreach (UserAlbumMeta album in albumsToSearch)
        {
            int readSize = 200;
            int from = 0;
            while (true)
            {
                List<Media>? albumItems = AlbumIndexHandler.GetAlbumItems(username, album.AlbumName, from, readSize);
                if (albumItems == null || albumItems.Count == 0) break;

                int mediaAlbumIndex = 0;
                foreach (Media item in albumItems)
                {
                    bool tagsMatch = true;
                    if (tagsArray.Length > 0)
                    {
                        if (allTagsMustMatch)
                        {
                            foreach (string tag in tagsArray)
                            {
                                if (item.Tags.Any(t => t.TagName == tag)) continue;
                                else tagsMatch = false;
                            }
                        }
                        else
                        {
                            tagsMatch = false;
                            foreach (string tag in tagsArray)
                            {
                                if (item.Tags.Any(t => t.TagName == tag))
                                {
                                    tagsMatch = true;
                                    break; // No need to check further if at least one tag matches
                                }
                            }
                        }
                    }

                    bool extensionMatch = fileExtensionsArray.Length == 0 || fileExtensionsArray.Any(ext => item.Name.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
                    bool mediaNameMatch = mediaNameContains is null || item.Name.Contains(mediaNameContains) || item.Id.Contains(mediaNameContains);

                    if (tagsMatch && extensionMatch && mediaNameMatch)
                    {
                        SearchHit searchHit = new()
                        {
                            AlbumName = album.AlbumName,
                            MediaAlbumIndex = mediaAlbumIndex,
                            MediaItem = item
                        };
                        hits.Add(searchHit);
                    }

                    mediaAlbumIndex++;
                    if (hits.Count >= maxSize) break;
                }

                from += readSize;
                if (hits.Count >= maxSize) break;
            }

            if (hits.Count >= maxSize) break;
        }

        return hits;
    }

    private static List<UserAlbumMeta> FilterListOfAlbums(List<UserAlbumMeta> allUserAlbums, string[] albumsArray, string[] tagsArray, bool allTagsMustMatch = true)
    {
        List<UserAlbumMeta> albumsToSearch = [];
        if (albumsArray.Length > 0)
        {
            foreach (UserAlbumMeta album in allUserAlbums)
            {
                if (albumsArray.Any(a => a == album.AlbumName)) albumsToSearch.Add(album);
            }
        }
        else albumsToSearch = allUserAlbums;    // We won't do any modifications so it's fine to use the same reference
        
        List<UserAlbumMeta> albumsWithTags = [];
        if (tagsArray.Length > 0)
        {
            foreach (UserAlbumMeta album in albumsToSearch)
            {
                bool tagsMatch = true;
                if (allTagsMustMatch)
                {
                    // All tags must be present in album
                    foreach (string tag in tagsArray)
                    {
                        if (album.Tags.Any(t => t.TagName == tag)) continue;
                        else tagsMatch = false;
                    }
                }
                else
                {
                    // At least one tag must be present in album
                    tagsMatch = false;
                    foreach (string tag in tagsArray)
                    {
                        if (album.Tags.Any(t => t.TagName == tag))
                        {
                            tagsMatch = true;
                            break;
                        }
                    }
                }

                if (tagsMatch) albumsWithTags.Add(album);
            }
        }

        if (albumsWithTags.Count > 0) albumsToSearch = albumsWithTags;

        return albumsToSearch;
    }
}