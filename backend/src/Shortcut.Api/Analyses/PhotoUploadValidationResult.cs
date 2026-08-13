namespace Shortcut.Api.Analyses;

public sealed record PhotoUploadValidationResult(bool IsValid, string ErrorType, string Message)
{
    public static PhotoUploadValidationResult Success { get; } = new(true, "", "");

    public static PhotoUploadValidationResult Failure(string errorType, string message) =>
        new(false, errorType, message);
}
