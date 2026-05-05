using System;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Rethink.Services.Domain.Interfaces;

namespace BillingService.Web;

/// <summary>
/// Deterministic Key Vault substitute for automated integration tests (no Azure credential required).
/// Activated when host environment name is <see cref="EnvironmentName"/>.
/// </summary>
public sealed class IntegrationTestKeyVaultProvider : IKeyVaultProviderService
{
    public const string EnvironmentName = "IntegrationTest";

    /// <summary>Placeholder SQL connection string for registration (tests mock DB/repos; health check may probe a dead server).</summary>
    public const string TestSqlConnectionString =
        "Server=127.0.0.1;Database=billing_test;User Id=test;Password=test;TrustServerCertificate=True;";

    public const string TestServiceBusConnectionString =
        "Endpoint=sb://test.servicebus.local/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test";

    public Task<string> GetSecretAsync(string secretName)
    {
        if (string.IsNullOrEmpty(secretName))
        {
            return Task.FromResult(string.Empty);
        }

        if (_secretNameToValue.TryGetValue(secretName, out var v))
        {
            return Task.FromResult(v);
        }

        return Task.FromResult($"secret-for-{secretName}");
    }

    /// <summary>Secret names match appsettings placeholders passed to Key Vault.</summary>
    private static readonly Dictionary<string, string> _secretNameToValue = new(StringComparer.Ordinal)
    {
        ["QA--Billing--ConnectionStrings--Database--UserId"] = "testuser",
        ["QA--Billing--ConnectionStrings--Database--Password"] = "testpass",
        ["QA--Billing--ConnectionStrings--ReportingDB--UserId"] = "testuser",
        ["QA--Billing--ConnectionStrings--ReportingDB--Password"] = "testpass",

        ["QA--Billing--ConnectionStrings--ServiceBus--ConnectionString"] = TestServiceBusConnectionString,
        ["QA--Billing--ConnectionStrings--BlobStorage--ConnectionString"] = "UseDevelopmentStorage=true;",
        ["QA--Billing--ConnectionStrings--RedisCache--ConnectionString"] = "127.0.0.1:6379,abortConnect=false",

        ["QA--Billing--ApplicationInsights--InstrumentationKey"] = "00000000-0000-0000-0000-000000000001",
        ["QA--Billing--ApplicationInsights--APPLICATIONINSIGHTS--CONNECTIONSTRING"] = string.Empty,

        ["QA--Billing--EdiFabric--SerialKey"] = "test-edi-key",

        ["QA--Billing--AccountsKey"] = "test-accounts",
        ["QA--Billing--CurriculumsKey"] = "test-curriculum",
        ["QA--Billing--DemographicsKey"] = "test-demographics",
        ["QA--Billing--HealthPlansKey"] = "test-healthplans",
        ["QA--Billing--HealthInsuranceKey"] = "test-hi",
        ["QA--Billing--MedicalRecordsKey"] = "test-mr",
        ["QA--Billing--PracticeOperationsKey"] = "test-po",
        ["QA--Billing--AppointmentAPIKey"] = "test-appt-api",
        ["QA--Billing--AppointmentApplicationKey"] = "test-appt-app",

        ["QA--Billing--PusherAppId"] = "test-app",
        ["QA--Billing--PusherKey"] = "test-key",
        ["QA--Billing--PusherSecret"] = "test-secret",

        ["QA--Billing--Jwt--Key"] = "INTEGRATION_TEST_JWT_KEY_MIN_32_CHARS_LONG_!!",
        ["QA--Billing--XApiKey"] = "B6E9430A-49E6-4100-AFC2-C37C50CFFB33",
    };

    public static bool IsEnabled(IHostEnvironment? env) =>
        env != null && string.Equals(env.EnvironmentName, EnvironmentName, StringComparison.Ordinal);
}
