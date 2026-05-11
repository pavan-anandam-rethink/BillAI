using ClearinghousePlugins.Abstractions;
using ClearingHouse.SharedKernel.Enums;
using ClearingHouse.SharedKernel.Models;
using Microsoft.Extensions.Logging;

namespace ClearinghousePlugins.Ability;

public class AbilityPlugin : IClearinghousePlugin
{
    public ClearinghouseType ClearinghouseType => ClearinghouseType.Ability;
    public string Name => "Ability";

    private readonly ILogger<AbilityPlugin> _logger;
    private ClearinghouseConnectionConfig? _config;

    public AbilityPlugin(ILogger<AbilityPlugin> logger) => _logger = logger;

    public Task<Result> InitializeAsync(ClearinghouseConnectionConfig config, CancellationToken cancellationToken = default)
    {
        _config = config;
        _logger.LogInformation("Initializing Ability plugin with host {Host}", config.Host);
        return Task.FromResult(Result.Success());
    }

    public Task<Result> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Testing Ability connection");
        return Task.FromResult(Result.Success());
    }
}
