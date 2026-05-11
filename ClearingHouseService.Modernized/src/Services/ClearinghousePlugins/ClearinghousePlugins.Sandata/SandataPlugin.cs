using ClearinghousePlugins.Abstractions;
using ClearingHouse.SharedKernel.Enums;
using ClearingHouse.SharedKernel.Models;
using Microsoft.Extensions.Logging;

namespace ClearinghousePlugins.Sandata;

public class SandataPlugin : IClearinghousePlugin
{
    public ClearinghouseType ClearinghouseType => ClearinghouseType.Sandata;
    public string Name => "Sandata";

    private readonly ILogger<SandataPlugin> _logger;
    private ClearinghouseConnectionConfig? _config;

    public SandataPlugin(ILogger<SandataPlugin> logger) => _logger = logger;

    public Task<Result> InitializeAsync(ClearinghouseConnectionConfig config, CancellationToken cancellationToken = default)
    {
        _config = config;
        _logger.LogInformation("Initializing Sandata plugin with host {Host}", config.Host);
        return Task.FromResult(Result.Success());
    }

    public Task<Result> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Testing Sandata connection");
        return Task.FromResult(Result.Success());
    }
}
