using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AiRulesEngine.Web.Services
{
    public interface IFileTextExtractor
    {
        Task<string> ExtractAsync(IFormFile file, CancellationToken cancellationToken = default);
    }
}
