using ClearingHouseService.Domain.Entities;
using ClearingHouseService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using System.Diagnostics;

namespace ClearingHouseService.Infrastructure.Transport
{
    /// <summary>
    /// SFTP-based transport implementation for clearing houses.
    /// Wraps the existing SFTP logic with the new transport abstraction.
    /// </summary>
    public class SftpTransport : IClearingHouseTransport
    {
        private readonly ILogger<SftpTransport> _logger;

        public SftpTransport(ILogger<SftpTransport> logger)
        {
            _logger = logger;
        }

        public async Task<TransmissionResult> SendAsync(ClearingHouseConfig config, string fileName, Stream data, CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation(
                    "Uploading file {FileName} to {Host}:{Port}{Directory} for ClearingHouse {ClearingHouseId}",
                    fileName, config.Host, config.Port, config.UploadDirectory, config.ClearingHouseId);

                using var client = CreateSftpClient(config);
                client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(180);
                client.Connect();

                var remotePath = config.UploadDirectory.TrimEnd('/') + "/" + fileName;

                await Task.Run(() => client.UploadFile(data, remotePath), cancellationToken);

                sw.Stop();
                _logger.LogInformation(
                    "Successfully uploaded {FileName} to {ClearingHouseName} in {DurationMs}ms",
                    fileName, config.Title, sw.ElapsedMilliseconds);

                return TransmissionResult.Success(fileName, sw.ElapsedMilliseconds);
            }
            catch (Renci.SshNet.Common.SshAuthenticationException ex)
            {
                sw.Stop();
                _logger.LogError(ex, "SFTP authentication failed for {ClearingHouseName}", config.Title);
                return TransmissionResult.Fail(TransmissionErrorType.AuthenticationFailure,
                    $"Authentication failed for {config.Title}: {ex.Message}");
            }
            catch (Renci.SshNet.Common.SshConnectionException ex)
            {
                sw.Stop();
                _logger.LogError(ex, "SFTP connection failed for {ClearingHouseName}", config.Title);
                return TransmissionResult.Fail(TransmissionErrorType.ConnectionFailure,
                    $"Connection failed for {config.Title}: {ex.Message}");
            }
            catch (TimeoutException ex)
            {
                sw.Stop();
                _logger.LogError(ex, "SFTP timeout for {ClearingHouseName}", config.Title);
                return TransmissionResult.Fail(TransmissionErrorType.Timeout,
                    $"Timeout connecting to {config.Title}: {ex.Message}");
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Unexpected error uploading to {ClearingHouseName}", config.Title);
                return TransmissionResult.Fail(TransmissionErrorType.UploadFailed,
                    $"Upload failed for {config.Title}: {ex.Message}");
            }
        }

        public async Task<List<(MemoryStream Data, string FileName)>> ReceiveAsync(ClearingHouseConfig config, CancellationToken cancellationToken = default)
        {
            var results = new List<(MemoryStream Data, string FileName)>();

            try
            {
                _logger.LogInformation(
                    "Downloading files from {Host}:{Port}{Directory} for ClearingHouse {ClearingHouseId}",
                    config.Host, config.Port, config.DownloadDirectory, config.ClearingHouseId);

                using var client = CreateSftpClient(config);
                client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(180);
                client.Connect();

                var files = client.ListDirectory(config.DownloadDirectory)
                    .Where(f => !f.IsDirectory && f.Name != "." && f.Name != "..");

                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var ms = new MemoryStream();
                    await Task.Run(() => client.DownloadFile(file.FullName, ms), cancellationToken);
                    ms.Position = 0;
                    results.Add((ms, file.Name));

                    _logger.LogInformation("Downloaded file {FileName} from {ClearingHouseName}", file.Name, config.Title);
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading files from {ClearingHouseName}", config.Title);
                throw;
            }
        }

        public async Task<TransmissionResult> ValidateConnectionAsync(ClearingHouseConfig config, CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                using var client = CreateSftpClient(config);
                client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(30);

                await Task.Run(() => client.Connect(), cancellationToken);

                var isConnected = client.IsConnected;
                client.Disconnect();

                sw.Stop();

                if (isConnected)
                {
                    _logger.LogInformation("Connection validation succeeded for {ClearingHouseName} in {DurationMs}ms",
                        config.Title, sw.ElapsedMilliseconds);
                    return TransmissionResult.Success(string.Empty, sw.ElapsedMilliseconds);
                }

                return TransmissionResult.Fail(TransmissionErrorType.ConnectionFailure,
                    $"Unable to connect to {config.Title}");
            }
            catch (Renci.SshNet.Common.SshAuthenticationException ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Credential validation failed for {ClearingHouseName}", config.Title);
                return TransmissionResult.Fail(TransmissionErrorType.AuthenticationFailure,
                    $"Authentication failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Connection validation failed for {ClearingHouseName}", config.Title);
                return TransmissionResult.Fail(TransmissionErrorType.ConnectionFailure,
                    $"Connection failed: {ex.Message}");
            }
        }

        public async Task<bool> DeleteAsync(ClearingHouseConfig config, string fileName, CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = CreateSftpClient(config);
                client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(180);
                client.Connect();

                var remotePath = config.DownloadDirectory.TrimEnd('/') + "/" + fileName;

                await Task.Run(() => client.DeleteFile(remotePath), cancellationToken);

                _logger.LogInformation("Deleted file {FileName} from {ClearingHouseName}", fileName, config.Title);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete {FileName} from {ClearingHouseName}", fileName, config.Title);
                return false;
            }
        }

        private static SftpClient CreateSftpClient(ClearingHouseConfig config)
        {
            return new SftpClient(config.Host, config.Port, config.UserName, config.UserPassword);
        }
    }
}
