using System;
using System.Collections.Generic;

namespace AiRulesEngine.Web.Models
{
    public class RuleSet
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; }
        public string Description { get; set; }
        public string SourceFileName { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public List<RuleDefinition> Rules { get; set; } = new();
    }

    public class RuleDefinition
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; }
        public string Message { get; set; }
        public RuleSeverity Severity { get; set; } = RuleSeverity.Error;
        public RuleMatchMode MatchMode { get; set; } = RuleMatchMode.All;
        public List<RuleCondition> Conditions { get; set; } = new();
    }

    public class RuleCondition
    {
        public string Field { get; set; }
        public RuleOperator Operator { get; set; } = RuleOperator.Equals;
        public string Value { get; set; }
        public List<string> Values { get; set; } = new();
    }

    public enum RuleMatchMode
    {
        All = 0,
        Any = 1
    }

    public enum RuleSeverity
    {
        Warning = 0,
        Error = 1
    }

    public enum RuleOperator
    {
        Required = 0,
        Equals = 1,
        NotEquals = 2,
        GreaterThan = 3,
        LessThan = 4,
        Regex = 5,
        Numeric = 6,
        LengthEquals = 7,
        LengthMin = 8,
        LengthMax = 9,
        In = 10
    }

    public class RuleViolation
    {
        public string RuleId { get; set; }
        public string Message { get; set; }
        public RuleSeverity Severity { get; set; }
        public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}
