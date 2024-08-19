namespace MinimalGallery.API;

using System.Text;
using System.Text.Json.Serialization;
using MinimalGallery.API.Models;

static class FileStorageHandler
{
    public static bool CreateNewUser(string storagePath, string userName)
    {
        string path = Path.Combine(storagePath, userName);
        if (Directory.Exists(path)) 
            return false;
        
        Directory.CreateDirectory(path);
        
        string userMeta = Path.Combine(path, $"{userName}_meta.dat");
        var metaData = new 
        {
            Name = userName,
            Created = DateTimeOffset.Now,
            AlbumsCount = 0,
        };

        string json = System.Text.Json.JsonSerializer.Serialize(metaData);
        File.WriteAllText(userMeta, json);

        return true;
    }

    public static void CreateNewAlbum(string storagePath, string userName, string albumName)
    {
        if (!PathExists(storagePath, userName)) throw new Exception($"The user '{userName}' doesn't exist. Create the user first.");
        string albumIndexName = Path.Combine(storagePath, userName, $"{albumName}.dat");
        if (File.Exists(albumIndexName)) throw new Exception($"The album '{albumName}' for user '{userName}' already exists.");
        using FileStream fs = File.Create(albumIndexName);
    }

    public static void AddNewMedia(string storagePath, string userName, string albumName, NewMediaRequest r)
    {
        string path = Path.Combine(storagePath, userName, $"{albumName}.dat");
        if (!File.Exists(path)) throw new Exception($"The album '{albumName}' for user '{userName}' doesn't exist. Create it first.");
        
        var mediaData = new Media 
        {
            Id = Guid.NewGuid().ToString(),
            Name = r.Name,
            Size = r.Size,
            Created = DateTimeOffset.Now
        };

        const int chunkSize = 1024*5;
        int chunkIndex = 0; // Example: update the 3rd chunk (index 2)
        byte[] newData = new byte[chunkSize]; // New data to write
        
        string json = System.Text.Json.JsonSerializer.Serialize(mediaData);
        json = json.Insert(json.LastIndexOf('}') + 1, "\n");
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
        if (jsonBytes.Length > chunkSize) throw new Exception("overflow!");
        jsonBytes.CopyTo(newData, 0);

        using (FileStream fs = new(path, FileMode.Open, FileAccess.Write))
        {
            fs.Seek(chunkIndex * chunkSize, SeekOrigin.Begin); // Move to the start of the chunk
            fs.Write(newData, 0, newData.Length); // Write the new data
        }
    }

    public static Media? GetMedia(string storagePath, string userName, string albumName, string searchTerm)
    {
        string path = Path.Combine(storagePath, userName, $"{albumName}.txt");
        if (!File.Exists(path)) throw new Exception($"The album '{albumName}' for user '{userName}' doesn't exist. Create it first.");

        foreach (string line in File.ReadLines(path))
        {
            if (line.Contains(searchTerm))
            {
                Media? media = System.Text.Json.JsonSerializer.Deserialize<Media>(line);
                
                return media;
            }
        }

        return null;
    }

    public static void AddTag()
    {
        // string path = Path.Combine(storagePath, userName, $"{albumName}.txt");
        // if (!File.Exists(path)) throw new Exception($"The album '{albumName}' for user '{userName}' doesn't exist. Create it first.");

    }

    private static bool PathExists(params string[] paths)
    {
        string path = Path.Combine(paths);
        return Directory.Exists(path);
    }
}