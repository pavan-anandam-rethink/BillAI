using AiRulesEngine.Web.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AiRulesEngine.Web.Services
{
    public interface IRuleRepository
    {
        Task<IReadOnlyList<RuleSet>> ListAsync(CancellationToken cancellationToken = default);
        Task<RuleSet> GetAsync(string id, CancellationToken cancellationToken = default);
        Task SaveAsync(RuleSet ruleSet, CancellationToken cancellationToken = default);
    }
}
