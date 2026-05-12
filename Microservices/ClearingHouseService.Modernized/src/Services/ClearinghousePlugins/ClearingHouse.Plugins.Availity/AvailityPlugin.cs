using ClearingHouse.Plugins.Contracts;
using ClearingHouse.SharedKernel.Domain;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Renci.SshNet;

namespace ClearingHouse.Plugins.Availity;

public class AvailityPlugin : IClearinghousePlugin, IDisposable
{
    private readonly AvailityConfiguration _config;
    private readonly ILogger<AvailityPlugin> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public string ClearinghouseId => "Availity";
    public string DisplayName => "Availity Health Information Network";
    public bool IsEnabled => _config.IsEnabled;

    public AvailityPlugin(AvailityConfiguration config, ILogger<AvailityPlugin> logger)
    {
        _config = config;
        _logger = logger;
        _retryPolicy = Policy.Handle<Exception>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retryCount, _) =>
                    _logger.LogWarning(exception, "Availity retry {RetryCount} after {Delay}s", retryCount, timeSpan.TotalSeconds));
    }

    public async Task<Result> ValidateConnectionAsync(CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            using var client = CreateSftpClient();
            await Task.Run(() => client.Connect(), cancellationToken);
            var isConnected = client.IsConnected;
            client.Disconnect();
            return isConnected ? Result.Success() : Result.Failure("Connection validation failed");
        });
    }

    public async Task<Result<SubmissionResult>> SubmitClaimAsync(ClaimSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            using var client = CreateSftpClient();
            await Task.Run(() => client.Connect(), cancellationToken);

            var remotePath = $"{_config.UploadDirectory}/{request.FileName}";
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(request.EdiContent));
            await Task.Run(() => client.UploadFile(stream, remotePath), cancellationToken);

            _logger.LogInformation("Submitted claim {FileName} to Availity at {RemotePath}", request.FileName, remotePath);

            return Result.Success(new SubmissionResult(true, Guid.NewGuid().ToString(), remotePath, null));
        });
    }

    public async Task<Result<IReadOnlyList<ResponseFile>>> RetrieveResponsesAsync(RetrieveResponsesRequest request, CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            using var client = CreateSftpClient();
            await Task.Run(() => client.Connect(), cancellationToken);

            var files = client.ListDirectory(_config.DownloadDirectory)
                .Where(f => !f.IsDirectory && f.Name != "." && f.Name != "..")
                .Where(f => request.Since == null || f.LastWriteTimeUtc >= request.Since)
                .Select(f =>
                {
                    var ms = new MemoryStream();
                    client.DownloadFile(f.FullName, ms);
                    ms.Position = 0;
                    return new ResponseFile(f.Name, f.FullName, ms, f.Length, f.LastWriteTimeUtc);
                })
                .ToList();

            _logger.LogInformation("Retrieved {Count} response files from Availity", files.Count);
            return Result.Success<IReadOnlyList<ResponseFile>>(files);
        });
    }

    public async Task<Result<EligibilityResponse>> CheckEligibilityAsync(EligibilityRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Eligibility check via Availity for subscriber {SubscriberId}", request.SubscriberId);
        await Task.CompletedTask;
        return Result.Failure<EligibilityResponse>("Eligibility via Availity requires REST API integration - not SFTP");
    }

    public async Task<Result> DeleteRemoteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            using var client = CreateSftpClient();
            await Task.Run(() => client.Connect(), cancellationToken);
            await Task.Run(() => client.DeleteFile(filePath), cancellationToken);
            return Result.Success();
        });
    }

    private SftpClient CreateSftpClient()
    {
        var connectionInfo = new ConnectionInfo(_config.Host, _config.Port, _config.Username,
            new PasswordAuthenticationMethod(_config.Username, _config.Password))
        {
            Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
        };
        return new SftpClient(connectionInfo);
    }

    public void Dispose() { }
}

public class AvailityConfiguration
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 22;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string UploadDirectory { get; set; } = "/outbound";
    public string DownloadDirectory { get; set; } = "/inbound";
    public int TimeoutSeconds { get; set; } = 30;
    public bool IsEnabled { get; set; } = true;
}
