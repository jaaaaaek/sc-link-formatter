using MooooosicMoooooocher.Models;

namespace MooooosicMoooooocher.Services
{
    public interface IDownloadService
    {
        Task<DownloadResult> DownloadAsync(
            DownloadItem item,
            string outputFolder,
            string? authToken,
            IProgress<DownloadProgress>? progress = null,
            CancellationToken cancellationToken = default);

        bool CancelDownload(Guid downloadId);
        bool IsDownloadActive(Guid downloadId);

        Task<IReadOnlyList<string>> ResolvePlaylistAsync(
            string url,
            string? authToken,
            IProgress<DownloadProgress>? progress = null,
            CancellationToken cancellationToken = default);
    }

    public record DownloadResult(bool Success, string? ErrorMessage, string? OutputFileName, bool WasSkipped = false);
}
