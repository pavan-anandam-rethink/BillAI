using Microsoft.AspNetCore.Http;

namespace AiRulesEngine.Web.Services;

public interface IFileTextExtractor
{
    Task<string> ExtractAsync(IFormFile file, CancellationToken cancellationToken);
}
