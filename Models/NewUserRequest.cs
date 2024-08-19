namespace MinimalGallery.API.Models;

record NewUserRequest
{
    public required string UserName {get;set;}
}