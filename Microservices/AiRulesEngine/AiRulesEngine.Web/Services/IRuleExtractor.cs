using AiRulesEngine.Web.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AiRulesEngine.Web.Services
{
    public interface IRuleExtractor
    {
        Task<IReadOnlyList<RuleDefinition>> ExtractRulesAsync(string content, CancellationToken cancellationToken = default);
    }
}
