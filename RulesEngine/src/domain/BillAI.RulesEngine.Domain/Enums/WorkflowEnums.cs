namespace BillAI.RulesEngine.Domain.Enums;

/// <summary>Lifecycle status of a workflow definition.</summary>
public enum WorkflowStatus
{
    /// <summary>Workflow is being designed and is not yet active.</summary>
    Draft = 0,

    /// <summary>Workflow is published and available for execution.</summary>
    Published = 1,

    /// <summary>Workflow has been archived.</summary>
    Archived = 2
}

/// <summary>The type of a workflow node.</summary>
public enum WorkflowNodeType
{
    Start,
    End,
    Decision,
    Validation,
    Parallel,
    ConditionalBranch,
    Timer,
    Delay,
    Notification,
    Approval,
    Loop,
    SubWorkflow,
    ErrorHandler
}

/// <summary>Runtime execution status of a workflow instance.</summary>
public enum WorkflowInstanceStatus
{
    Pending,
    Running,
    Succeeded,
    Failed,
    Suspended,
    Cancelled,
    TimedOut
}
