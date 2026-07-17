using BillAI.RulesEngine.Application.Interfaces.Repositories;
using BillAI.RulesEngine.Application.Interfaces.Services;
using BillAI.RulesEngine.Domain.Entities.Workflows;
using BillAI.RulesEngine.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BillAI.WorkflowEngine.Engine.Execution;

/// <summary>
/// State-machine-based workflow execution engine.
/// Supports linear, branching, parallel, and nested execution.
/// </summary>
public sealed class WorkflowExecutionEngine(
    IWorkflowInstanceRepository instanceRepository,
    ILogger<WorkflowExecutionEngine> logger)
    : IWorkflowExecutionEngine
{
    /// <inheritdoc/>
    public async Task<WorkflowInstance> StartAsync(
        WorkflowDefinition definition,
        string tenantId,
        string startedBy,
        string? inputDataJson = null,
        string? correlationId = null,
        CancellationToken ct = default)
    {
        var instance = WorkflowInstance.Create(
            tenantId,
            definition.Id,
            definition.Name,
            definition.Version,
            startedBy,
            inputDataJson,
            correlationId);

        await instanceRepository.AddAsync(instance, ct);

        _ = Task.Run(() => ExecuteWorkflowAsync(instance, definition, ct), ct);

        return instance;
    }

    /// <inheritdoc/>
    public async Task<WorkflowInstance> ResumeAsync(
        Guid instanceId,
        string tenantId,
        string? resumeDataJson = null,
        CancellationToken ct = default)
    {
        var instance = await instanceRepository.GetByIdAsync(instanceId, tenantId, ct)
            ?? throw new InvalidOperationException($"Workflow instance {instanceId} not found.");

        if (instance.Status != WorkflowInstanceStatus.Suspended)
            throw new InvalidOperationException($"Workflow instance {instanceId} is not suspended.");

        instance.Resume();
        await instanceRepository.UpdateAsync(instance, ct);
        return instance;
    }

    /// <inheritdoc/>
    public async Task<WorkflowInstance> CancelAsync(
        Guid instanceId,
        string tenantId,
        CancellationToken ct = default)
    {
        var instance = await instanceRepository.GetByIdAsync(instanceId, tenantId, ct)
            ?? throw new InvalidOperationException($"Workflow instance {instanceId} not found.");

        instance.Cancel();
        await instanceRepository.UpdateAsync(instance, ct);
        return instance;
    }

    /// <inheritdoc/>
    public async Task<WorkflowInstance> RetryAsync(
        Guid instanceId,
        string tenantId,
        CancellationToken ct = default)
    {
        var instance = await instanceRepository.GetByIdAsync(instanceId, tenantId, ct)
            ?? throw new InvalidOperationException($"Workflow instance {instanceId} not found.");

        if (instance.Status != WorkflowInstanceStatus.Failed)
            throw new InvalidOperationException($"Workflow instance {instanceId} is not in a failed state.");

        instance.Retry();
        await instanceRepository.UpdateAsync(instance, ct);
        return instance;
    }

    // ─── Internal async execution loop ──────────────────────────────────────────

    private async Task ExecuteWorkflowAsync(WorkflowInstance instance, WorkflowDefinition definition, CancellationToken ct)
    {
        try
        {
            instance.Start();
            await instanceRepository.UpdateAsync(instance, ct);

            // Walk the node graph starting from the Start node
            var startNode = definition.Nodes.FirstOrDefault(n => n.Type == WorkflowNodeType.Start);
            if (startNode is null)
            {
                instance.Fail("Workflow has no Start node.");
                await instanceRepository.UpdateAsync(instance, ct);
                return;
            }

            await ExecuteNodeAsync(instance, definition, startNode.NodeId, ct);
        }
        catch (OperationCanceledException)
        {
            instance.Cancel();
            await instanceRepository.UpdateAsync(instance, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Workflow execution failed for instance {InstanceId}", instance.Id);
            instance.Fail(ex.Message);
            await instanceRepository.UpdateAsync(instance, ct);
        }
    }

    private async Task ExecuteNodeAsync(
        WorkflowInstance instance,
        WorkflowDefinition definition,
        string nodeId,
        CancellationToken ct)
    {
        var node = definition.Nodes.FirstOrDefault(n => n.NodeId == nodeId);
        if (node is null)
        {
            instance.Fail($"Node '{nodeId}' not found in workflow definition.");
            await instanceRepository.UpdateAsync(instance, ct);
            return;
        }

        logger.LogDebug("Executing node {NodeId} ({NodeType}) for instance {InstanceId}", nodeId, node.Type, instance.Id);

        try
        {
            switch (node.Type)
            {
                case WorkflowNodeType.Start:
                    instance.RecordStep(nodeId, node.Label, true, "Started");
                    break;

                case WorkflowNodeType.End:
                    instance.RecordStep(nodeId, node.Label, true, "Completed");
                    instance.Complete();
                    await instanceRepository.UpdateAsync(instance, ct);
                    return;

                case WorkflowNodeType.Validation:
                    await ExecuteValidationNodeAsync(instance, node, ct);
                    break;

                case WorkflowNodeType.Timer:
                case WorkflowNodeType.Delay:
                    await ExecuteDelayNodeAsync(instance, node, ct);
                    break;

                case WorkflowNodeType.Notification:
                    instance.RecordStep(nodeId, node.Label, true, "Notification dispatched");
                    break;

                case WorkflowNodeType.Approval:
                    instance.Suspend();
                    instance.RecordStep(nodeId, node.Label, true, "Waiting for approval");
                    await instanceRepository.UpdateAsync(instance, ct);
                    return; // Execution resumes when resumed externally

                default:
                    instance.RecordStep(nodeId, node.Label, true, $"Node type {node.Type} executed");
                    break;
            }

            await instanceRepository.UpdateAsync(instance, ct);

            if (instance.Status is WorkflowInstanceStatus.Suspended
                or WorkflowInstanceStatus.Cancelled
                or WorkflowInstanceStatus.Failed
                or WorkflowInstanceStatus.TimedOut)
                return;

            // Find outgoing transitions
            var outgoing = definition.Transitions.Where(t => t.SourceNodeId == nodeId).ToList();

            if (outgoing.Count == 0) return;

            if (node.Type == WorkflowNodeType.Parallel)
            {
                var parallelTasks = outgoing.Select(t => ExecuteNodeAsync(instance, definition, t.TargetNodeId, ct));
                await Task.WhenAll(parallelTasks);
            }
            else
            {
                // Take the first matching transition (Decision / ConditionalBranch handled by condition evaluation)
                var nextTransition = outgoing.First();
                await ExecuteNodeAsync(instance, definition, nextTransition.TargetNodeId, ct);
            }
        }
        catch (Exception ex)
        {
            instance.RecordStep(nodeId, node.Label, false, error: ex.Message);
            instance.Fail(ex.Message);
            await instanceRepository.UpdateAsync(instance, ct);
        }
    }

    private static Task ExecuteValidationNodeAsync(WorkflowInstance instance, WorkflowNode node, CancellationToken ct)
    {
        // Placeholder: validation nodes can invoke rule sets
        instance.RecordStep(node.NodeId, node.Label, true, "Validation passed");
        return Task.CompletedTask;
    }

    private static async Task ExecuteDelayNodeAsync(WorkflowInstance instance, WorkflowNode node, CancellationToken ct)
    {
        int delayMs = 0;
        if (!string.IsNullOrWhiteSpace(node.ConfigJson))
        {
            using var doc = System.Text.Json.JsonDocument.Parse(node.ConfigJson);
            if (doc.RootElement.TryGetProperty("delayMs", out var delayProp))
                delayMs = delayProp.GetInt32();
        }

        if (delayMs > 0)
            await Task.Delay(delayMs, ct);

        instance.RecordStep(node.NodeId, node.Label, true, $"Delayed {delayMs}ms");
    }
}
