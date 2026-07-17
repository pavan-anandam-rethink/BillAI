using BillAI.RulesEngine.Domain.Entities.Rules;
using BillAI.RulesEngine.Domain.Enums;
using BillAI.RulesEngine.Domain.Events.Rules;
using Xunit;

namespace BillAI.RulesEngine.Domain.Tests;

public sealed class RuleDefinitionTests
{
    private const string TenantId = "tenant-1";
    private const string CreatedBy = "user@example.com";

    [Fact]
    public void Create_ShouldReturnDraftRule_WithCorrectProperties()
    {
        var rule = RuleDefinition.Create(TenantId, "TestRule", "Claims", "{}", CreatedBy, priority: 50);

        Assert.Equal(TenantId, rule.TenantId);
        Assert.Equal("TestRule", rule.Name);
        Assert.Equal("Claims", rule.Category);
        Assert.Equal(RuleStatus.Draft, rule.Status);
        Assert.Equal(1, rule.Version);
        Assert.Equal(50, rule.Priority);
        Assert.True(rule.IsEnabled);
    }

    [Fact]
    public void Create_ShouldRaiseRuleCreatedEvent()
    {
        var rule = RuleDefinition.Create(TenantId, "TestRule", "Claims", "{}", CreatedBy);
        var events = rule.PopDomainEvents();

        Assert.Single(events);
        Assert.IsType<RuleCreatedEvent>(events[0]);
    }

    [Fact]
    public void Publish_ShouldTransitionToPublished()
    {
        var rule = RuleDefinition.Create(TenantId, "TestRule", "Claims", "{}", CreatedBy);
        rule.Publish("publisher@example.com");

        Assert.Equal(RuleStatus.Published, rule.Status);
    }

    [Fact]
    public void Publish_ShouldRaiseRulePublishedEvent()
    {
        var rule = RuleDefinition.Create(TenantId, "TestRule", "Claims", "{}", CreatedBy);
        rule.PopDomainEvents(); // Clear creation event
        rule.Publish("publisher@example.com");

        var events = rule.PopDomainEvents();
        Assert.Single(events);
        Assert.IsType<RulePublishedEvent>(events[0]);
    }

    [Fact]
    public void Update_ShouldIncrementVersionAndCreateSnapshot()
    {
        var rule = RuleDefinition.Create(TenantId, "TestRule", "Claims", "{\"original\":true}", CreatedBy);
        rule.Update("TestRule", "Claims", "{\"updated\":true}", "updater@example.com");

        Assert.Equal(2, rule.Version);
        Assert.Single(rule.Versions);
        Assert.Equal(1, rule.Versions[0].VersionNumber);
    }

    [Fact]
    public void Rollback_ShouldRestoreVersionContent()
    {
        var rule = RuleDefinition.Create(TenantId, "TestRule", "Claims", "{\"v1\":true}", CreatedBy);
        rule.Update("TestRuleV2", "Claims", "{\"v2\":true}", "updater@example.com");
        rule.Rollback(1, "admin@example.com");

        Assert.Equal("TestRule", rule.Name);
        Assert.Equal("{\"v1\":true}", rule.RuleExpression);
        Assert.Equal(RuleStatus.Draft, rule.Status); // Rolled back to draft
    }

    [Fact]
    public void Rollback_WithInvalidVersion_ShouldThrow()
    {
        var rule = RuleDefinition.Create(TenantId, "TestRule", "Claims", "{}", CreatedBy);
        Assert.Throws<InvalidOperationException>(() => rule.Rollback(99, "admin@example.com"));
    }

    [Fact]
    public void Archive_ShouldDisableAndSetArchived()
    {
        var rule = RuleDefinition.Create(TenantId, "TestRule", "Claims", "{}", CreatedBy);
        rule.Archive("admin@example.com");

        Assert.Equal(RuleStatus.Archived, rule.Status);
        Assert.False(rule.IsEnabled);
    }

    [Fact]
    public void AddTag_ShouldNormaliseAndStoreLowerCase()
    {
        var rule = RuleDefinition.Create(TenantId, "TestRule", "Claims", "{}", CreatedBy);
        rule.AddTag("  Healthcare  ");

        Assert.Single(rule.Tags);
        Assert.Equal("healthcare", rule.Tags[0].Value);
    }

    [Fact]
    public void AddTag_DuplicateTag_ShouldNotAddTwice()
    {
        var rule = RuleDefinition.Create(TenantId, "TestRule", "Claims", "{}", CreatedBy);
        rule.AddTag("healthcare");
        rule.AddTag("HEALTHCARE");

        Assert.Single(rule.Tags);
    }

    [Fact]
    public void PopDomainEvents_ShouldClearAfterPop()
    {
        var rule = RuleDefinition.Create(TenantId, "TestRule", "Claims", "{}", CreatedBy);
        var events1 = rule.PopDomainEvents();
        var events2 = rule.PopDomainEvents();

        Assert.Single(events1);
        Assert.Empty(events2);
    }
}
