namespace MinimalGallery.API.Models;

record DeleteTagRequest
{
    public required string TagName {get;set;}
}