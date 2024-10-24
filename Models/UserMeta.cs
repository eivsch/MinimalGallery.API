namespace MinimalGallery.API.Models;

record UserMeta
{
    public required string Username {get;set;}
    public required string Password {get;set;}
    public required List<UserAlbumMeta> AlbumMeta {get;set;}
    public required DateTime Created {get;set;}
}

record UserAlbumMeta
{
    public required string AlbumName {get;set;}
    public required DateTime Created {get;set;}
    public required List<UserAlbumTagMeta> Tags {get;set;}
    public int TotalLikes {get;set;}       // sum of all likes
    public int TotalUniqueLikes {get;set;} // each unique media item that has been liked
    public int? TotalCount{get;set;}    // TODO: remove from serialization as this is set dynamically
}

record UserAlbumTagMeta
{
    public required string TagName {get;set;}
    public required int Count {get;set;}
}