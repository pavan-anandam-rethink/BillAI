using ClearingHouse.SharedKernel.Models;
using StediIntegration.Domain.Entities;

namespace StediIntegration.Domain.Interfaces;

public interface IStediApiClient
{
    Task<Result<string>> SubmitTransactionAsync(Stream ediContent, string transactionType, CancellationToken cancellationToken = default);
    Task<Result<string>> GetTransactionStatusAsync(string transactionId, CancellationToken cancellationToken = default);
    Task<Result<Stream>> DownloadResponseAsync(string transactionId, CancellationToken cancellationToken = default);
    Task<Result> ValidateConnectionAsync(CancellationToken cancellationToken = default);
}
