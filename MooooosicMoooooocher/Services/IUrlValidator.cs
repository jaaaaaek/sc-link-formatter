namespace MooooosicMoooooocher.Services
{
    public interface IUrlValidator
    {
        UrlValidationResult Validate(string url, IReadOnlyCollection<string>? existingUrls = null);
        bool IsResolvableUrl(string url);
    }

    public readonly record struct UrlValidationResult(bool IsValid, string Message);
}
