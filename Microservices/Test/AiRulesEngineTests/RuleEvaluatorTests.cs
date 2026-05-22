using AiRulesEngine.Web.Models;
using AiRulesEngine.Web.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace AiRulesEngine.Tests;

public class RuleEvaluatorTests
{
    [Fact]
    public void Evaluate_WhenRequiredFieldMissing_ReturnsViolation()
    {
        var ruleSet = new RuleSet
        {
            Id = "ruleset-1",
            Rules =
            {
                new RuleDefinition
                {
                    Id = "rule-1",
                    Name = "Billing provider NPI required",
                    Message = "Billing provider NPI is required.",
                    Severity = RuleSeverity.Error,
                    Conditions =
                    {
                        new RuleCondition
                        {
                            Field = "BillingProvider.NpiNumber",
                            Operator = RuleOperator.Required
                        }
                    }
                }
            }
        };

        var context = new ClaimRulesEngineContext
        {
            BillingProvider = new BillingProviderContext { NpiNumber = "" }
        };

        var violations = RuleEvaluator.Evaluate(ruleSet, context);

        Assert.Single(violations);
        Assert.Equal("rule-1", violations[0].RuleId);
    }

    [Fact]
    public void Evaluate_WhenConditionsMet_ReturnsNoViolations()
    {
        var ruleSet = new RuleSet
        {
            Id = "ruleset-2",
            Rules =
            {
                new RuleDefinition
                {
                    Id = "rule-zip",
                    Name = "Billing zip length",
                    Message = "Billing zip must be 9 digits.",
                    Severity = RuleSeverity.Error,
                    Conditions =
                    {
                        new RuleCondition
                        {
                            Field = "BillingProviderAddress.Zip",
                            Operator = RuleOperator.LengthEquals,
                            Value = "9"
                        },
                        new RuleCondition
                        {
                            Field = "BillingProviderAddress.Zip",
                            Operator = RuleOperator.Numeric
                        }
                    }
                }
            }
        };

        var context = new ClaimRulesEngineContext
        {
            BillingProviderAddress = new AddressContext { Zip = "123456789" }
        };

        var violations = RuleEvaluator.Evaluate(ruleSet, context);

        Assert.Empty(violations);
    }
}

public class FileTextExtractorTests
{
    [Fact]
    public async Task ExtractAsync_ReadsPlainText()
    {
        var extractor = new FileTextExtractor();
        var content = "Sample rule text";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var file = new FormFile(stream, 0, stream.Length, "file", "rules.txt");

        var extracted = await extractor.ExtractAsync(file);

        Assert.Equal(content, extracted);
    }

    [Fact]
    public async Task ExtractAsync_ReadsPdf()
    {
        var extractor = new FileTextExtractor();
        var pdfPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "..",
            "docs", "ai-rules-engine", "samples", "sample-rules.pdf"));
        await using var stream = File.OpenRead(pdfPath);
        var file = new FormFile(stream, 0, stream.Length, "file", "sample-rules.pdf");

        var extracted = await extractor.ExtractAsync(file);

        Assert.Contains("Billing Provider ZIP", extracted);
    }
}
