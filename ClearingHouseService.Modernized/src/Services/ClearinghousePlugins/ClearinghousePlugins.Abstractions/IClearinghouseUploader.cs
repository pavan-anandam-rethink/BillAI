using ClearingHouse.SharedKernel.Models;

namespace ClearinghousePlugins.Abstractions;

public interface IClearinghouseUploader
{
    Task<Result<string>> UploadFileAsync(Stream content, string fileName, ClearinghousePluginContext context, CancellationToken cancellationToken = default);
}
