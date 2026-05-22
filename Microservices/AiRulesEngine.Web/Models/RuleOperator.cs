namespace AiRulesEngine.Web.Models;

public enum RuleOperator
{
    Required,
    Equals,
    NotEquals,
    Regex,
    Numeric,
    LengthEquals,
    LengthBetween,
    DateWithinDays
}
