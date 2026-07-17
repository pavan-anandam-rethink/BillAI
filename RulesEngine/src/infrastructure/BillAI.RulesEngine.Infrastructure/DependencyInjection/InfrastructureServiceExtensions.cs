using Azure.Storage.Blobs;
using BillAI.RulesEngine.Application.Interfaces.Repositories;
using BillAI.RulesEngine.Application.Interfaces.Services;
using BillAI.RulesEngine.Infrastructure.Repositories;
using BillAI.RulesEngine.Infrastructure.Storage.Azure;
using BillAI.RulesEngine.Infrastructure.Storage.Local;
using BillAI.RulesEngine.Infrastructure.Tenant;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BillAI.RulesEngine.Infrastructure.DependencyInjection;

/// <summary>
/// Extension methods for registering Infrastructure layer services.
/// </summary>
public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// Registers infrastructure services including storage provider, repositories, and tenant service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Storage provider — switch between Azure Blob and local file storage
        var azureConnectionString = configuration.GetConnectionString("AzureStorage");

        if (!string.IsNullOrWhiteSpace(azureConnectionString))
        {
            services.AddSingleton(new BlobServiceClient(azureConnectionString));
            services.AddSingleton<IStorageProvider, AzureBlobStorageProvider>();
        }
        else
        {
            var rootPath = configuration["Storage:LocalRootPath"]
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BillAI.RulesEngine");

            services.AddSingleton<IStorageProvider>(_ => new LocalFileStorageProvider(rootPath));
        }

        // Repositories
        services.AddSingleton<IRuleRepository, StorageBackedRuleRepository>();
        services.AddSingleton<IWorkflowRepository, StorageBackedWorkflowRepository>();
        services.AddSingleton<IWorkflowInstanceRepository, StorageBackedWorkflowInstanceRepository>();
        services.AddSingleton<IClaimValidationRepository, StorageBackedClaimValidationRepository>();

        // Tenant service
        services.AddScoped<ITenantService, HttpContextTenantService>();

        return services;
    }
}
