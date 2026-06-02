using BillingService.Domain.Interfaces.PatientInvoice;
using BillingService.Domain.Interfaces.Payment;
using BillingService.Domain.Models.Claims;
using BillingService.LegacyAdapters.Claims;
using BillingService.LegacyAdapters.PatientInvoice;
using BillingService.LegacyAdapters.Payments;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace BillingService.LegacyAdapters;

public static class DependencyInjection
{
    public static IServiceCollection AddBillingLegacyAdapters(this IServiceCollection services)
    {
        // Claim compatibility
        services.AddScoped<IClaimCompatibilityFacade, ClaimCompatibilityFacade>();

        // MediatR handlers that wrap legacy domain services.
        // Registration is explicit here so that Application.DependencyInjection does not need to
        // reference the LegacyAdapters assembly (which would create a circular dependency).
        services.AddScoped<IRequestHandler<GetClaimHeadersQuery, ClaimHeaderModelResponseModel>,
            GetClaimHeadersQueryHandler>();

        // Payment compatibility
        services.AddScoped<IPaymentCompatibilityFacade, PaymentCompatibilityFacade>();

        // Patient invoice compatibility
        services.AddScoped<IPatientInvoiceCompatibilityFacade, PatientInvoiceCompatibilityFacade>();

        return services;
    }
}
