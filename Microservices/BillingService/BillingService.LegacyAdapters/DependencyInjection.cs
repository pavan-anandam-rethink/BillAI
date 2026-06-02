using BillingService.Application.Abstractions.Claims;
using BillingService.LegacyAdapters.Claims;
using BillingService.LegacyAdapters.PatientInvoice;
using BillingService.LegacyAdapters.Payment;
using Microsoft.Extensions.DependencyInjection;

namespace BillingService.LegacyAdapters;

public static class DependencyInjection
{
    public static IServiceCollection AddBillingLegacyAdapters(this IServiceCollection services)
    {
        services.AddScoped<IClaimCompatibilityFacade, ClaimCompatibilityFacade>();
        services.AddScoped<IPaymentCompatibilityFacade, PaymentCompatibilityFacade>();
        services.AddScoped<IPatientInvoiceCompatibilityFacade, PatientInvoiceCompatibilityFacade>();

        // Application-layer ports implemented by legacy adapters.
        services.AddScoped<IClaimReader, ClaimReaderAdapter>();

        return services;
    }
}
