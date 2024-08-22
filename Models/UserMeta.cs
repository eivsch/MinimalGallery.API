namespace MinimalGallery.API.Models;

record UserMeta
{
    public required string Username {get;set;}
    public required List<UserTagMeta> TagMeta {get;set;}
    public required List<UserAlbumMeta> AlbumMeta {get;set;}
    public required DateTime Created {get;set;}
}

record UserAlbumMeta
{
    public required string AlbumName {get;set;}
    public required DateTime Created {get;set;}
}

record UserTagMeta
{
    public required string TagName {get;set;}
    public required int Count {get;set;}
}