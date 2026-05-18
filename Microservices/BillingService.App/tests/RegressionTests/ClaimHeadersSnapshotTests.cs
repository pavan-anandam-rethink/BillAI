using System.Text.Json;

namespace BillingService.App.RegressionTests;

public sealed class ClaimHeadersSnapshotTests
{
    [Fact]
    public void SnapshotContainsExpectedTopLevelFields()
    {
        var snapshotPath = Path.Combine(AppContext.BaseDirectory, "Snapshots", "claim-getheaders.response.json");
        var json = File.ReadAllText(snapshotPath);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("result", out _));
        Assert.True(root.TryGetProperty("totalRecords", out _));
        Assert.True(root.TryGetProperty("errors", out _));
    }
}

