using ClearinghousePlugins.Abstractions;
using ClearingHouse.SharedKernel.Enums;
using ClearingHouse.SharedKernel.Models;
using Microsoft.Extensions.Logging;

namespace ClearinghousePlugins.Waystar;

public class WaystarPlugin : IClearinghousePlugin
{
    public ClearinghouseType ClearinghouseType => ClearinghouseType.Waystar;
    public string Name => "Waystar";

    private readonly ILogger<WaystarPlugin> _logger;
    private ClearinghouseConnectionConfig? _config;

    public WaystarPlugin(ILogger<WaystarPlugin> logger) => _logger = logger;

    public Task<Result> InitializeAsync(ClearinghouseConnectionConfig config, CancellationToken cancellationToken = default)
    {
        _config = config;
        _logger.LogInformation("Initializing Waystar plugin with host {Host}", config.Host);
        return Task.FromResult(Result.Success());
    }

    public Task<Result> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Testing Waystar connection");
        return Task.FromResult(Result.Success());
    }
}
