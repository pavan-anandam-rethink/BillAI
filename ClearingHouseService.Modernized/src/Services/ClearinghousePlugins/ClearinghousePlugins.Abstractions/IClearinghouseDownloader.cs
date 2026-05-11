using ClearingHouse.SharedKernel.Models;

namespace ClearinghousePlugins.Abstractions;

public interface IClearinghouseDownloader
{
    Task<Result<IReadOnlyList<string>>> ListAvailableFilesAsync(ClearinghousePluginContext context, CancellationToken cancellationToken = default);
    Task<Result<Stream>> DownloadFileAsync(string fileName, ClearinghousePluginContext context, CancellationToken cancellationToken = default);
}
