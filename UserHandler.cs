namespace MinimalGallery.API;

using System.Text.Json;
using MinimalGallery.API.Models;

static class UserHandler
{
    public static bool CreateNewUser(string userName)
    {
        string path = Path.Combine(FileStorageHandler.StoragePath, userName);
        if (Directory.Exists(path)) 
            return false;
        
        Directory.CreateDirectory(path);

        UserMeta metaData = new()
        {
            Username = userName,
            AlbumMeta = [],
            TagMeta = [],
            Created = DateTime.UtcNow,
        };

        WriteUser(metaData);

        return true;
    }

    public static UserMeta? GetUserMeta(string username)
    {
        string path = FileStorageHandler.GetPathUser(username);
        if (!File.Exists(path)) return null;

        UserMeta? user = ReadUser(username);

        return user;
    }

    public static bool DeleteUser(string username)
    {
        string path = Path.Combine(FileStorageHandler.StoragePath, username);
        if (!Directory.Exists(path)) return false;
        
        Directory.Delete(path, recursive: true);

        return true;
    }

    public static UserMeta? ReadUser(string username)
    {
        string path = FileStorageHandler.GetPathUser(username);
        string content = File.ReadAllText(path);
        UserMeta? userMeta = JsonSerializer.Deserialize<UserMeta>(content);

        return userMeta;
    }

    public static void WriteUser(UserMeta userMeta)
    {
        string file = FileStorageHandler.GetPathUser(userMeta.Username);
        string json = JsonSerializer.Serialize(userMeta);
        File.WriteAllText(file, json);
    }
}