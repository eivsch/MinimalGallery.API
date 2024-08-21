namespace MinimalGallery.API;

using System.Text;
using System.Text.Json;
using MinimalGallery.API.Models;

static class FileStorageHandler
{
    const string END_TAG = "<END>";
    const string FILE_EXT = "dat";
    const int CHUNK_SIZE = 1024*2;

    public static string StoragePath {get;set;} = "not set";
    
    public static bool CreateNewUser(string userName)
    {
        string path = Path.Combine(StoragePath, userName);
        if (Directory.Exists(path)) 
            return false;
        
        Directory.CreateDirectory(path);
        
        string userMeta = Path.Combine(path, $"{userName}_meta.{FILE_EXT}");
        var metaData = new 
        {
            Name = userName,
            Created = DateTimeOffset.Now,
            AlbumsCount = 0,
        };

        string json = JsonSerializer.Serialize(metaData);
        File.WriteAllText(userMeta, json);

        return true;
    }

    public static void CreateNewAlbum(string username, string albumName)
    {
        if (!PathExists(StoragePath, username)) throw new Exception($"The user '{username}' doesn't exist. Create the user first.");
        string albumIndexName = GetPathAlbum(username, albumName);
        
        if (File.Exists(albumIndexName)) throw new Exception($"The album '{albumName}' for user '{username}' already exists.");
        using FileStream fs = File.Create(albumIndexName);
    }

    public static void AddNewMedia(string username, string albumName, NewMediaRequest r)
    {
        string path = GetPathAlbum(username, albumName);
        if (!File.Exists(path)) CreateNewAlbum(username, albumName);
        
        var mediaData = new Media 
        {
            Id = Guid.NewGuid().ToString(),
            Name = r.Name,
            Size = r.Size,
            Created = DateTimeOffset.Now
        };

        byte[] newData = GetDataAsByteChunk(mediaData);
        using FileStream fs = new(path, FileMode.Open, FileAccess.Write);
        fs.Seek(0, SeekOrigin.End);
        fs.Write(newData, 0, newData.Length);
    }

    public static Media? GetMedia(string username, string albumName, string searchTerm)
    {
        string path = GetPathAlbum(username, albumName);
        if (!File.Exists(path)) throw new Exception($"The album '{albumName}' for user '{username}' doesn't exist. Create it first.");

        foreach (string line in File.ReadLines(path))
        {
            if (line.Contains(searchTerm))
            {
                Media? media = DeserializeMediaString(line);
                
                return media;
            }
        }

        return null;
    }

    public static void AddTag(string username, string albumName, string searchTerm, NewTagRequest r)
    {
        string path = GetPathAlbum(username, albumName);
        if (!File.Exists(path)) throw new Exception($"Add tag failed. The album '{albumName}' for user '{username}' doesn't exist.");

        int? i = GetChunkIndex(path, searchTerm) ?? throw new Exception($"Add tag failed. Cannot find '{searchTerm}' in album '{albumName}'.");

        Tag t = new() { TagName = r.TagName, Created = DateTimeOffset.Now };

        using FileStream fs = new(path, FileMode.Open, FileAccess.ReadWrite);
        Media? media = ReadMediaChunk(fs, i.Value) ?? throw new Exception("Add tag failed. Unable to read media item from file.");
        media.Tags.Add(t);
        byte[] asBytes = GetDataAsByteChunk(media);
        fs.Write(asBytes, 0, asBytes.Length);
    }

    // helpers
    private static int? GetChunkIndex(string path, string searchTerm)
    {
        int chunkIndex = 0;
        byte[] buffer = new byte[CHUNK_SIZE];
        using (FileStream fs = new(path, FileMode.Open, FileAccess.Read))
        {
            while (true)
            {
                int offset = chunkIndex*CHUNK_SIZE;
                if (offset > fs.Length-CHUNK_SIZE) break;

                fs.Seek(offset, SeekOrigin.Begin);
                int n = fs.Read(buffer, 0, CHUNK_SIZE);
                string chunkStr = Encoding.UTF8.GetString(buffer);
                if (chunkStr.Contains(searchTerm))
                {
                    return chunkIndex;
                }

                chunkIndex++;
            }
        }

        return null;
    }

    private static Media? ReadMediaChunk(FileStream fs, int i)
    {
        if (!fs.CanRead) throw new Exception("Unable to read from the provided file stream. File stream must be opened in either read or read-write mode.");

        byte[] readBuffer = new byte[CHUNK_SIZE];
        int offset = i * CHUNK_SIZE;
        fs.Seek(offset, SeekOrigin.Begin);
        int n = fs.Read(readBuffer, 0, CHUNK_SIZE);
        
        string chunkStr = Encoding.UTF8.GetString(readBuffer);
        Media? media = DeserializeMediaString(chunkStr);

        return media;
    }

    private static Media? DeserializeMediaString(string mediaStr)
    {
        int endIndex = mediaStr.IndexOf(END_TAG);
        mediaStr = mediaStr[..endIndex];
        Media? media = JsonSerializer.Deserialize<Media>(mediaStr);

        return media;
    }

    private static byte[] GetDataAsByteChunk(Media data)
    {
        byte[] dataChunk = new byte[CHUNK_SIZE];
        
        // serialize to json
        string json = JsonSerializer.Serialize(data);
        json += END_TAG;
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
        if (jsonBytes.Length > CHUNK_SIZE-2) throw new Exception($"Overflow error: Maximum chunk size for a media record is {CHUNK_SIZE-2}. Current size is {jsonBytes.Length}.");
        jsonBytes.CopyTo(dataChunk, 0);
        
        // add a line ending after padding
        byte[] lineEnding = Encoding.UTF8.GetBytes("\n");
        byte l = lineEnding[0];
        dataChunk[dataChunk.Length-1] = l;

        return dataChunk;
    }

    private static string GetPathAlbum(string username, string albumName)
    {
        string path = Path.Combine(StoragePath, username, $"{albumName}.{FILE_EXT}");

        return path;
    }

    private static bool PathExists(params string[] paths)
    {
        string path = Path.Combine(paths);
        return Directory.Exists(path);
    }
}