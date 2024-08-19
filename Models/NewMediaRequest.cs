namespace MinimalGallery.API.Models;

record NewMediaRequest
{
    public required string Name {get;set;}
    public required string MediaType {get;set;}
    public int? Size {get;set;}
}