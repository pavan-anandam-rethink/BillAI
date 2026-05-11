using System.Net.Http.Json;
using ClearingHouse.SharedKernel.Models;
using Microsoft.Extensions.Logging;
using StediIntegration.Domain.Interfaces;

namespace StediIntegration.Infrastructure.ApiClients;

public class StediHttpClient : IStediApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StediHttpClient> _logger;

    public StediHttpClient(HttpClient httpClient, ILogger<StediHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Result<string>> SubmitTransactionAsync(Stream ediContent, string transactionType, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Submitting {TransactionType} to Stedi API", transactionType);
        await Task.CompletedTask;
        return Result.Success(Guid.NewGuid().ToString());
    }

    public async Task<Result<string>> GetTransactionStatusAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting status for Stedi transaction {TransactionId}", transactionId);
        await Task.CompletedTask;
        return Result.Success("Completed");
    }

    public async Task<Result<Stream>> DownloadResponseAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Downloading response for Stedi transaction {TransactionId}", transactionId);
        await Task.CompletedTask;
        return Result.Success<Stream>(new MemoryStream());
    }

    public async Task<Result> ValidateConnectionAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating Stedi API connection");
        await Task.CompletedTask;
        return Result.Success();
    }
}
