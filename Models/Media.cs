namespace MinimalGallery.API.Models;

record Media
{
    public required string Id {get;set;}
    public required string Name {get;set;}
    public required DateTimeOffset Created {get;set;}
    public int? Size {get;set;}
}