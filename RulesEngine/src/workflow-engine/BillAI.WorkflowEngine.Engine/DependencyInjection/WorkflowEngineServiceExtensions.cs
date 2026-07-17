using BillAI.RulesEngine.Application.Interfaces.Services;
using BillAI.WorkflowEngine.Engine.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace BillAI.WorkflowEngine.Engine.DependencyInjection;

/// <summary>
/// Extension methods for registering Workflow Engine services.
/// </summary>
public static class WorkflowEngineServiceExtensions
{
    /// <summary>Registers the workflow execution engine and related services.</summary>
    public static IServiceCollection AddWorkflowEngine(this IServiceCollection services)
    {
        services.AddScoped<IWorkflowExecutionEngine, WorkflowExecutionEngine>();
        return services;
    }
}
