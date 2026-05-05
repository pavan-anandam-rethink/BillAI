using Authentication.Interfaces;
using Authentication.Services;
using BillingService.Domain.Services.RethinkMasterDataMicroservices;
using HealthChecks.UI.Client;
using LoginService.Web.Interfaces;
using LoginService.Web.Repositories.NoSql;
using LoginService.Web.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Rethink.Services.Common.Cache;
using Rethink.Services.Common.Cache.Redis;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Domain.Configuration;
using Rethink.Services.Domain.Interfaces;
using Rethink.Services.Domain.Services.RethinkServices;
using RethinkCore.Common.Logging.Extensions;
using RethinkCore.Common.MongoDB;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", true, true)
    .AddJsonFile($"appsettings.{environmentName}.json", true, true)
    .AddEnvironmentVariables();

builder.Services.AddSingleton<IKeyVaultProviderService, KeyVaultProviderService>();
var keyVaultProvider = builder.Services.BuildServiceProvider().GetRequiredService<IKeyVaultProviderService>();
var redisConnectionString = await keyVaultProvider.GetSecretAsync(config["RedisCache:ConnectionString"]!);

builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks().AddAzureApplicationInsights(config["ApplicationInsights:InstrumentationKey"], name: "App Insights");
builder.Services.ConfigureMongoDB(config);
builder.Services.AddMongoRepository<UserProfileRepository>();
builder.Services.AddTransient<ITokenService, TokenService>();
builder.Services.AddScoped<ICacheManager, RedisCacheManager>();
builder.Services.AddTransient<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IUserProfileRepository, UserProfileRepository>();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

RethinkMicroserviceHttpClientsRegistration.Register(builder.Services, config, keyVaultProvider);

builder.Services.AddHttpClient("tokenValidation", client =>
{
    client.BaseAddress = new Uri(config["TokenValidationApi"]!.TrimEnd('/') + "/");
});

builder.Services.AddScoped<IRethinkBillingRequestContext, RethinkBillingRequestContext>();
builder.Services.AddSingleton<IRethinkMasterDataSessionCache, RethinkMasterDataSessionCache>();
builder.Services.AddScoped<IRethinkMasterDataSessionPrewarm, RethinkMasterDataSessionPrewarm>();
builder.Services.AddScoped<IRethinkMasterDataMicroServices, RethinkMasterDataMicroServices>();

builder.Services.AddRethinkLogging(config);
builder.Logging.AddAppInsightLogger(config);

var app = builder.Build();

app.UseCors(options => options.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.UseSwagger();
app.UseSwaggerUI();
app.UseHealthChecks("/api/health", new HealthCheckOptions()
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponseNoExceptionDetails
});
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
await app.RunAsync();
