using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SftpIngestion.Application.Commands;

namespace SftpIngestion.Infrastructure.Workers;

public class SftpPollingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SftpPollingWorker> _logger;
    private readonly string[] _clearinghouseIds = { "Stedi", "Availity", "TriZetto", "Sandata", "Waystar" };
    private readonly TimeSpan _pollingInterval = TimeSpan.FromMinutes(5);

    public SftpPollingWorker(IServiceScopeFactory scopeFactory, ILogger<SftpPollingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SFTP Polling Worker starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var tasks = _clearinghouseIds.Select(id => PollClearinghouseAsync(id, stoppingToken));
                await Task.WhenAll(tasks);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error during SFTP polling cycle");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }

    private async Task PollClearinghouseAsync(string clearinghouseId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var correlationId = Guid.NewGuid().ToString();

        await mediator.Send(new PollClearinghouseCommand(clearinghouseId, correlationId), cancellationToken);
    }
}
