using ClaimLifecycleMonitoring.Application.Abstractions.RuleEngine;
using ClaimLifecycleMonitoring.Application.Abstractions.Time;
using ClaimLifecycleMonitoring.Application.Common;
using ClaimLifecycleMonitoring.Application.Common.Behaviors;
using ClaimLifecycleMonitoring.Application.Configuration;
using ClaimLifecycleMonitoring.Application.Monitoring;
using ClaimLifecycleMonitoring.Application.Monitoring.Rules;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClaimLifecycleMonitoring.Application.DependencyInjection;

/// <summary>DI helpers for the Application layer.</summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>Registers Application services (MediatR, validators, rules, monitoring service).</summary>
    public static IServiceCollection AddClaimLifecycleApplication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<MonitoringOptions>()
            .Bind(configuration.GetSection(MonitoringOptions.SectionName));

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ApplicationServiceCollectionExtensions).Assembly);
        });

        services.AddValidatorsFromAssembly(typeof(ApplicationServiceCollectionExtensions).Assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddScoped<IMonitoringRule, StageSlaBreachRule>();
        services.AddScoped<IMonitoringRule, GlobalTimeoutRule>();
        services.AddScoped<IMonitoringRule, Missing999Rule>();
        services.AddScoped<IMonitoringRule, Missing277CaRule>();
        services.AddScoped<IMonitoringRule, Missing835Rule>();
        services.AddScoped<IMonitoringRule, UnalertedFailureRule>();
        services.AddScoped<IRuleEngine, CompositeRuleEngine>();

        services.AddScoped<IClaimMonitoringService, ClaimMonitoringService>();

        return services;
    }
}
