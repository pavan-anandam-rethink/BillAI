using BillingService.LegacyAdapters.Claims;
using BillingService.LegacyAdapters.ClearingHouse;
using BillingService.LegacyAdapters.PatientInvoice;
using BillingService.LegacyAdapters.Payment;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace BillingService.LegacyAdapters;

public static class DependencyInjection
{
    public static IServiceCollection AddBillingLegacyAdapters(this IServiceCollection services)
    {
        // Compatibility facades: wrap existing domain services for CQRS handler injection
        // and future Application-layer extraction. Each facade preserves the original
        // domain service contract with zero behavioral change.
        services.AddScoped<IClaimCompatibilityFacade, ClaimCompatibilityFacade>();
        services.AddScoped<IPaymentCompatibilityFacade, PaymentCompatibilityFacade>();
        services.AddScoped<IPatientInvoiceCompatibilityFacade, PatientInvoiceCompatibilityFacade>();
        services.AddScoped<IClearingHouseCompatibilityFacade, ClearingHouseCompatibilityFacade>();

        // Register CQRS handlers defined in this assembly (GetClaimHeadersQuery,
        // SaveClaimCommand, etc.). These complement the Application layer handlers
        // registered by AddBillingApplication and share the same MediatR pipeline.
        services.AddMediatR(options =>
            options.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        return services;
    }
}
