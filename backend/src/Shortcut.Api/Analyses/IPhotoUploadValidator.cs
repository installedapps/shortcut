namespace Shortcut.Api.Analyses;

public interface IPhotoUploadValidator
{
    Task<PhotoUploadValidationResult> ValidateAsync(
        IFormFile? photo,
        CancellationToken cancellationToken);
}
