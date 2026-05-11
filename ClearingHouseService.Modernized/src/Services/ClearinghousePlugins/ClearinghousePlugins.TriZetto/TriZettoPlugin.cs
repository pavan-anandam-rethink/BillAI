using ClearinghousePlugins.Abstractions;
using ClearingHouse.SharedKernel.Enums;
using ClearingHouse.SharedKernel.Models;
using Microsoft.Extensions.Logging;

namespace ClearinghousePlugins.TriZetto;

public class TriZettoPlugin : IClearinghousePlugin
{
    public ClearinghouseType ClearinghouseType => ClearinghouseType.TriZetto;
    public string Name => "TriZetto";

    private readonly ILogger<TriZettoPlugin> _logger;
    private ClearinghouseConnectionConfig? _config;

    public TriZettoPlugin(ILogger<TriZettoPlugin> logger) => _logger = logger;

    public Task<Result> InitializeAsync(ClearinghouseConnectionConfig config, CancellationToken cancellationToken = default)
    {
        _config = config;
        _logger.LogInformation("Initializing TriZetto plugin with host {Host}", config.Host);
        return Task.FromResult(Result.Success());
    }

    public Task<Result> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Testing TriZetto connection");
        return Task.FromResult(Result.Success());
    }
}
