namespace MinimalGallery.API;

static class Globals
{
    public const string END_TAG = "<END>";
    public const string FILE_EXT = "dat";
    public const int CHUNK_SIZE = 1024*2;

    public static string StoragePath {get;set;} = "not set";
}