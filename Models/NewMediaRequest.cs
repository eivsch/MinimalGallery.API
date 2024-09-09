namespace MinimalGallery.API.Models;

record NewMediaRequest
{
    public required string Name {get;set;}
    public int? Size {get;set;}
}