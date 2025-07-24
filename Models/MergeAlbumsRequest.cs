namespace MinimalGallery.API.Models;

record MergeAlbumsRequest
{
    public required string AlbumName1 { get; set; }
    public required string AlbumName2 { get; set; }
    public required string AlbumNameTarget { get; set; }
}