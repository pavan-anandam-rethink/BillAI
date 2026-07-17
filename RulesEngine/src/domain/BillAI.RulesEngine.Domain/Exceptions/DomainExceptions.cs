namespace BillAI.RulesEngine.Domain.Exceptions;

/// <summary>Thrown when a domain invariant is violated.</summary>
public class DomainException(string message) : Exception(message);

/// <summary>Thrown when a requested rule is not found.</summary>
public class RuleNotFoundException(Guid ruleId)
    : DomainException($"Rule with ID '{ruleId}' was not found.");

/// <summary>Thrown when a requested rule is not found by name.</summary>
public class RuleByNameNotFoundException(string name, string tenantId)
    : DomainException($"Rule '{name}' was not found for tenant '{tenantId}'.");

/// <summary>Thrown when a requested workflow is not found.</summary>
public class WorkflowNotFoundException(Guid workflowId)
    : DomainException($"Workflow with ID '{workflowId}' was not found.");

/// <summary>Thrown when a workflow instance is not found.</summary>
public class WorkflowInstanceNotFoundException(Guid instanceId)
    : DomainException($"Workflow instance with ID '{instanceId}' was not found.");

/// <summary>Thrown when an operation is attempted on a rule in an incompatible state.</summary>
public class InvalidRuleStateException(string message) : DomainException(message);

/// <summary>Thrown when rule evaluation fails due to an expression error.</summary>
public class RuleEvaluationException(string message, Exception? inner = null)
    : DomainException(message)
{
    public Exception? InnerCause { get; } = inner;
}
