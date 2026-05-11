using ClearinghousePlugins.Abstractions;
using ClearingHouse.SharedKernel.Enums;
using ClearingHouse.SharedKernel.Models;
using Microsoft.Extensions.Logging;

namespace ClearinghousePlugins.Stedi;

public class StediPlugin : IClearinghousePlugin
{
    public ClearinghouseType ClearinghouseType => ClearinghouseType.Stedi;
    public string Name => "Stedi";

    private readonly ILogger<StediPlugin> _logger;
    private ClearinghouseConnectionConfig? _config;

    public StediPlugin(ILogger<StediPlugin> logger) => _logger = logger;

    public Task<Result> InitializeAsync(ClearinghouseConnectionConfig config, CancellationToken cancellationToken = default)
    {
        _config = config;
        _logger.LogInformation("Initializing Stedi plugin with host {Host}", config.Host);
        return Task.FromResult(Result.Success());
    }

    public Task<Result> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Testing Stedi connection");
        return Task.FromResult(Result.Success());
    }
}
