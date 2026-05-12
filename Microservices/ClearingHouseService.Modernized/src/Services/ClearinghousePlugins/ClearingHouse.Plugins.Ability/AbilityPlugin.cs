using ClearingHouse.Plugins.Contracts;
using ClearingHouse.SharedKernel.Domain;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Renci.SshNet;

namespace ClearingHouse.Plugins.Ability;

public class AbilityPlugin : IClearinghousePlugin, IDisposable
{
    private readonly AbilityConfiguration _config;
    private readonly ILogger<AbilityPlugin> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public string ClearinghouseId => "Ability";
    public string DisplayName => "Ability Network (Inovalon)";
    public bool IsEnabled => _config.IsEnabled;

    public AbilityPlugin(AbilityConfiguration config, ILogger<AbilityPlugin> logger)
    {
        _config = config;
        _logger = logger;
        _retryPolicy = Policy.Handle<Exception>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    public async Task<Result> ValidateConnectionAsync(CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            using var client = CreateSftpClient();
            await Task.Run(() => client.Connect(), cancellationToken);
            var result = client.IsConnected ? Result.Success() : Result.Failure("Ability connection failed");
            client.Disconnect();
            return result;
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
            _logger.LogInformation("Submitted {FileName} to Ability", request.FileName);
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
                .Select(f =>
                {
                    var ms = new MemoryStream();
                    client.DownloadFile(f.FullName, ms);
                    ms.Position = 0;
                    return new ResponseFile(f.Name, f.FullName, ms, f.Length, f.LastWriteTimeUtc);
                }).ToList();
            return Result.Success<IReadOnlyList<ResponseFile>>(files);
        });
    }

    public Task<Result<EligibilityResponse>> CheckEligibilityAsync(EligibilityRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Failure<EligibilityResponse>("Ability eligibility not supported via SFTP"));
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

    private SftpClient CreateSftpClient() =>
        new(new ConnectionInfo(_config.Host, _config.Port, _config.Username,
            new PasswordAuthenticationMethod(_config.Username, _config.Password))
        { Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds) });

    public void Dispose() { }
}

public class AbilityConfiguration
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
