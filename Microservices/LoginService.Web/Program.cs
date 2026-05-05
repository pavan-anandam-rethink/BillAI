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
using Rethink.Services.Domain.Interfaces;
using RethinkCore.Common.Logging.Extensions;
using RethinkCore.Common.MongoDB;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
// Add services to the container.

var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", true, true)
    .AddJsonFile($"appsettings.{environmentName}.json", true, true)
    .AddEnvironmentVariables();

builder.Services.AddSingleton<IKeyVaultProviderService, KeyVaultProviderService>();
var keyVaultProvider = builder.Services.BuildServiceProvider().GetRequiredService<IKeyVaultProviderService>();
var redisConnectionString = keyVaultProvider.GetSecretAsync(config["RedisCache:ConnectionString"]).Result;

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    return ConnectionMultiplexer.Connect(redisConnectionString);
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks().AddAzureApplicationInsights(config["ApplicationInsights:InstrumentationKey"], name: "App Insights");
builder.Services.ConfigureMongoDB(config);
builder.Services.AddMongoRepository<UserProfileRepository>();
builder.Services.AddTransient<ITokenService, TokenService>();
builder.Services.AddScoped<ICacheManager, RedisCacheManager>();
builder.Services.AddTransient<IUserProfileService, UserProfileService>();
builder.Services.AddTransient<IRethinkMasterDataMicroServices, RethinkMasterDataMicroServices>();
builder.Services.AddScoped<IUserProfileRepository, UserProfileRepository>();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

var accountsKey = config["AccountsKey"];
builder.Services.AddHttpClient("accountsClient", client =>
{
    client.BaseAddress = new Uri(config["AccountsApiUrl"].ToString());
    client.DefaultRequestHeaders.Add(config["HeaderKey"].ToString(), accountsKey);
});


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
app.Run();