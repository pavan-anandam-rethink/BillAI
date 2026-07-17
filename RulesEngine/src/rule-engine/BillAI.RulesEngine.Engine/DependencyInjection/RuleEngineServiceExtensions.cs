using BillAI.RulesEngine.Application.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BillAI.RulesEngine.Engine.DependencyInjection;

/// <summary>
/// Extension methods for registering the Rules Runtime Engine.
/// </summary>
public static class RuleEngineServiceExtensions
{
    /// <summary>Registers the rules evaluation engine and related services.</summary>
    public static IServiceCollection AddRuleEngine(this IServiceCollection services)
    {
        services.AddSingleton<IRulesEvaluationEngine, Evaluators.RulesEvaluationEngine>();
        return services;
    }
}
