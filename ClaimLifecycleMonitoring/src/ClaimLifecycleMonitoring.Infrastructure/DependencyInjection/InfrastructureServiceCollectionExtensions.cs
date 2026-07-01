using ClaimLifecycleMonitoring.Application.Abstractions.Notifications;
using ClaimLifecycleMonitoring.Infrastructure.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace ClaimLifecycleMonitoring.Infrastructure.DependencyInjection;

/// <summary>DI helpers for the Infrastructure layer.</summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>Registers the notification publisher wired to a real-time broadcaster.</summary>
    /// <remarks>
    /// The host is responsible for registering an implementation of
    /// <see cref="IRealtimeClaimBroadcaster"/>. When no broadcaster is registered
    /// callers can register <see cref="NullRealtimeClaimBroadcaster"/> for a safe no-op.
    /// </remarks>
    public static IServiceCollection AddClaimLifecycleInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IClaimNotificationPublisher, RealtimeClaimNotificationPublisher>();
        return services;
    }

    /// <summary>Registers the <see cref="NullRealtimeClaimBroadcaster"/> as the transport.</summary>
    public static IServiceCollection AddNullRealtimeBroadcaster(this IServiceCollection services)
    {
        services.AddSingleton<IRealtimeClaimBroadcaster, NullRealtimeClaimBroadcaster>();
        return services;
    }
}
