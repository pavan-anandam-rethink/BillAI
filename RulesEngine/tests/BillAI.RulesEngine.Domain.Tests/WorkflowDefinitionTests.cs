using BillAI.RulesEngine.Domain.Entities.Workflows;
using BillAI.RulesEngine.Domain.Enums;
using BillAI.RulesEngine.Domain.Events.Workflows;
using Xunit;

namespace BillAI.RulesEngine.Domain.Tests;

public sealed class WorkflowDefinitionTests
{
    private const string TenantId = "tenant-1";
    private const string CreatedBy = "user@example.com";

    [Fact]
    public void Create_ShouldReturnDraftWorkflow()
    {
        var workflow = WorkflowDefinition.Create(TenantId, "ClaimWorkflow", "Claims", CreatedBy);

        Assert.Equal(TenantId, workflow.TenantId);
        Assert.Equal("ClaimWorkflow", workflow.Name);
        Assert.Equal(WorkflowStatus.Draft, workflow.Status);
        Assert.Equal(1, workflow.Version);
    }

    [Fact]
    public void AddNode_ShouldAddNodeToWorkflow()
    {
        var workflow = WorkflowDefinition.Create(TenantId, "ClaimWorkflow", "Claims", CreatedBy);
        workflow.AddNode("start-1", WorkflowNodeType.Start, "Start");

        Assert.Single(workflow.Nodes);
    }

    [Fact]
    public void AddNode_DuplicateNodeId_ShouldThrow()
    {
        var workflow = WorkflowDefinition.Create(TenantId, "ClaimWorkflow", "Claims", CreatedBy);
        workflow.AddNode("start-1", WorkflowNodeType.Start, "Start");

        Assert.Throws<InvalidOperationException>(() =>
            workflow.AddNode("start-1", WorkflowNodeType.Start, "Duplicate"));
    }

    [Fact]
    public void Publish_WithStartAndEnd_ShouldSucceed()
    {
        var workflow = WorkflowDefinition.Create(TenantId, "ClaimWorkflow", "Claims", CreatedBy);
        workflow.AddNode("start-1", WorkflowNodeType.Start, "Start");
        workflow.AddNode("end-1", WorkflowNodeType.End, "End");
        workflow.AddTransition("start-1", "end-1");

        workflow.Publish("publisher@example.com");

        Assert.Equal(WorkflowStatus.Published, workflow.Status);
    }

    [Fact]
    public void Publish_WithoutStartNode_ShouldThrow()
    {
        var workflow = WorkflowDefinition.Create(TenantId, "ClaimWorkflow", "Claims", CreatedBy);
        workflow.AddNode("end-1", WorkflowNodeType.End, "End");

        Assert.Throws<InvalidOperationException>(() => workflow.Publish("publisher@example.com"));
    }

    [Fact]
    public void Publish_WithoutEndNode_ShouldThrow()
    {
        var workflow = WorkflowDefinition.Create(TenantId, "ClaimWorkflow", "Claims", CreatedBy);
        workflow.AddNode("start-1", WorkflowNodeType.Start, "Start");

        Assert.Throws<InvalidOperationException>(() => workflow.Publish("publisher@example.com"));
    }

    [Fact]
    public void UpdateDefinition_ShouldIncrementVersionAndSetDraft()
    {
        var workflow = WorkflowDefinition.Create(TenantId, "ClaimWorkflow", "Claims", CreatedBy);
        workflow.UpdateDefinition("{\"updated\":true}", "updater@example.com");

        Assert.Equal(2, workflow.Version);
        Assert.Equal(WorkflowStatus.Draft, workflow.Status);
    }

    [Fact]
    public void Create_ShouldRaiseWorkflowCreatedEvent()
    {
        var workflow = WorkflowDefinition.Create(TenantId, "ClaimWorkflow", "Claims", CreatedBy);
        var events = workflow.PopDomainEvents();

        Assert.Single(events);
        Assert.IsType<WorkflowCreatedEvent>(events[0]);
    }
}
