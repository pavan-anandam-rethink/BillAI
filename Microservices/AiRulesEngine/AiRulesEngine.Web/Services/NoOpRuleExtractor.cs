using AiRulesEngine.Web.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AiRulesEngine.Web.Services
{
    public class NoOpRuleExtractor : IRuleExtractor
    {
        public Task<IReadOnlyList<RuleDefinition>> ExtractRulesAsync(string content, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("AI rule extraction is not configured. Provide RulesEngine:Ai settings.");
        }
    }
}
