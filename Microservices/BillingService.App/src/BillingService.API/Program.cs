using BillingService.App.Application;
using BillingService.App.API.Security;
using BillingService.App.Domain;
using BillingService.App.Infrastructure;
using BillingService.App.Infrastructure.Observability;
using BillingService.App.LegacyAdapters;
using BillingService.App.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<BillingModernizationSettings>(
    builder.Configuration.GetSection(BillingModernizationSettings.SectionName));

builder.Services.AddBillingApplication();
builder.Services.AddBillingPersistence(builder.Configuration);
builder.Services.AddBillingInfrastructure(builder.Configuration);
builder.Services.AddLegacyBillingAdapters(builder.Configuration);

builder.Services.AddControllers();
builder.Services
    .AddAuthentication("HeaderPassthrough")
    .AddScheme<AuthenticationSchemeOptions, HeaderPassthroughAuthHandler>("HeaderPassthrough", _ => { });
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddSlidingWindowLimiter("default", limiter =>
    {
        limiter.PermitLimit = 500;
        limiter.Window = TimeSpan.FromSeconds(10);
        limiter.SegmentsPerWindow = 2;
        limiter.QueueLimit = 200;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCorrelationId();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapHealthChecks("/api/health");
app.MapControllers().RequireRateLimiting("default");

var modernizationSettings = app.Services.GetRequiredService<IOptions<BillingModernizationSettings>>().Value;
if (modernizationSettings.UseLegacyProxyFallback)
{
    // All non-migrated routes are forwarded to the legacy BillingService host.
    app.MapReverseProxy();
}

app.Run();

