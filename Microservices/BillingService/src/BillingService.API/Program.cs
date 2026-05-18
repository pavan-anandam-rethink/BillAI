var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/api/health");
app.MapGet("/", () => Results.Ok(new
{
    service = "BillingService.API",
    mode = "clean-architecture-transition",
    compatibility = "Legacy endpoints remain hosted by BillingService.Web until each workflow is migrated."
}));

app.Run();
