
using ClearingHouseService.Web.Interface;
using ClearingHouseService.Web.Models;
using Renci.SshNet;
using Renci.SshNet.Common;
using Rethink.Services.Common.Enums.Billing;
using System.Diagnostics;

namespace ClearingHouseService.Web.Service
{
    // This class implements the IClearingHouseUploader interface to provide functionality for uploading files to a clearing house using SFTP.
    public class SftpUploader : IClearingHouseUploader
    {
        private readonly ICredentialResolver _credentialResolver;
        private readonly ILogger<SftpUploader> _logger;
        private readonly IConfiguration _configuration;
        private const int DefaultMaxConcurrency = 5;

        public SftpUploader(
            ICredentialResolver credentialResolver,
            ILogger<SftpUploader> logger,
            IConfiguration configuration)
        {
            _credentialResolver = credentialResolver;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// generic clearing house file uploader using SFTP. It will resolve the credentials based on the clearing house details and then upload the file to the specified SFTP server.
        /// </summary>

        public async Task<OperationResult> UploadFileToSftpAsync(ClearingHouseDetailsModel clearingHouse, string fileName, Stream fileStream,int claimId)
        {
            _logger.LogInformation($"UploadFileToSftpAsync Started ClearingHouseId: {clearingHouse.ClearingHouseId},claimid:{claimId}");

             var options = await _credentialResolver.ResolveAsync(clearingHouse);

            _logger.LogInformation($"UploadFileToSftpAsync ClearingHouseId: {clearingHouse.ClearingHouseId},claimid:{claimId},UrlLink:{options.UrlLink},Port:{options.Port},UserName:{options.UserName},UserPassword:{options.UserPassword}");
            try
            {
                using var client = new SftpClient(options.UrlLink, options.Port, options.UserName, options.UserPassword);

                client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(Convert.ToInt32(_configuration["SFTPConnectionTimeout"]));

                client.Connect();

                if (!client.IsConnected)
                {
                    _logger.LogInformation($"UploadFileToSftpAsync SFTP connection failed ClearingHouseId: {clearingHouse.ClearingHouseId},claimid:{claimId}");
                    return OperationResult.Fail(ErrorType.ConnectionFailure,"SFTP connection failed.");
                }                

                fileStream.Position = 0;

                _logger.LogInformation($"UploadFileToSftpAsync SFTP connection success ClearingHouseId: {clearingHouse.ClearingHouseId},claimid:{claimId}");
                var remotePath = $"{options.UploadDirectory}/{fileName}";

                client.UploadFile(fileStream, remotePath);

                client.Disconnect();
                _logger.LogInformation($"UploadFileToSftpAsync Ended ClearingHouseId: {clearingHouse.ClearingHouseId},claimid:{claimId}");

                return OperationResult.Success(fileName);
            }
            catch (SshAuthenticationException ex)
            {
                _logger.LogError(ex, $"Authentication failure for claim: {claimId}");
                return OperationResult.Fail(ErrorType.AuthFailure,"AUTH_FAILURE");
            }
            catch (SshConnectionException ex)
            {
                _logger.LogError(ex, $"Connection failure for claim: {claimId}");
                return OperationResult.Fail(ErrorType.ConnectionFailure,"CONNECTION_FAILURE");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected failure for claim: {claimId}");
                return OperationResult.Fail(ErrorType.UploadFailed,"UPLOAD_FAILED");
            }
        }

        /// <summary>
        /// Download file from Sftp 
        /// </summary>

        public async Task<List<(MemoryStream, string)>> DownloadFilesFromSftpAsync(ClearingHouseDetailsModel clearingHouse)
        {
            var fileStreams = new List<(MemoryStream, string)>();

            _logger.LogInformation($"DownloadFilesFromSftpAsync Started ClearingHouseId: {clearingHouse.ClearingHouseId}");

            var options = await _credentialResolver.ResolveAsync(clearingHouse);

            _logger.LogInformation($"DownloadFilesFromSftpAsync ClearingHouseId: {clearingHouse.ClearingHouseId},UrlLink:{options.UrlLink},Port:{options.Port},UserName:{options.UserName},UserPassword:{options.UserPassword}");

            using var sftpClient = new SftpClient(options.UrlLink, options.Port, options.UserName, options.UserPassword);

            sftpClient.ConnectionInfo.Timeout = TimeSpan.FromSeconds(Convert.ToInt32(_configuration["SFTPConnectionTimeout"]));

            sftpClient.Connect();

            if (!sftpClient.IsConnected)
            {
                var msg = $"SFTP client is not connected. ClearingHouseId:{clearingHouse.ClearingHouseId}, Host: {clearingHouse.UrlLink}, Port: {clearingHouse.Port}.";
                _logger.LogInformation(msg);
                return fileStreams;
            }
            _logger.LogInformation($"DownloadFilesFromSftpAsync SFTP client is connected ClearingHouseId: {clearingHouse.ClearingHouseId},DownloadDirectory:{options.DownloadDirectory}");

            var downloadedFileList = sftpClient.ListDirectory(options.DownloadDirectory)
                               .Where(f => !f.Name.StartsWith("."))
                               .ToList();

            _logger.LogInformation($"DownloadFilesFromSftpAsync ClearingHouseId: {clearingHouse.ClearingHouseId},downloadedFileList:{downloadedFileList.Count}");

            foreach (var file in downloadedFileList)
            {
                var stream = new MemoryStream();
                sftpClient.DownloadFile(file.FullName, stream);
                stream.Position = 0;
                fileStreams.Add((stream, file.Name));
            }

            _logger.LogInformation($"DownloadFilesFromSftpAsync Ended ClearingHouseId: {clearingHouse.ClearingHouseId},downloadedFileList:{downloadedFileList.Count}");

            return fileStreams;
        }

        /// <summary>
        /// Delete File from Sftp
        /// </summary>       
        public async Task<bool> DeleteFileFromSftpAsync(ClearingHouseDetailsModel clearingHouse, string fileName)
        {
            try
            {
                _logger.LogInformation($"DeleteFileFromSftpAsync Started ClearingHouseId: {clearingHouse.ClearingHouseId}");

                var options = await _credentialResolver.ResolveAsync(clearingHouse);

                _logger.LogInformation($"DeleteFileFromSftpAsync Started ClearingHouseId: {clearingHouse.ClearingHouseId},UrlLink:{options.UrlLink},Port:{options.Port},UserName:{options.UserName},UserPassword:{options.UserPassword}");

                using (var sftpClient = new SftpClient(options.UrlLink, options.Port, options.UserName, options.UserPassword))
                {
                    sftpClient.Connect();

                    if (sftpClient.IsConnected)
                    {
                        _logger.LogInformation($"DeleteFileFromSftpAsync SFTP Connected ClearingHouseId: {clearingHouse.ClearingHouseId},DownloadDirectory:{options.DownloadDirectory},filename:{fileName}");

                        sftpClient.DeleteFile($"{options.DownloadDirectory}/{fileName}");
                        sftpClient.Disconnect();
                    }
                }
                _logger.LogInformation($"DeleteFileFromSftpAsync Ended with Deleted ClearingHouseId: {clearingHouse.ClearingHouseId}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting file {fileName} from SFTP server");
                return false;
            }
        }

        /// <summary>
        /// Validates SFTP credentials for a single clearinghouse
        /// </summary>
        public async Task<ClearinghouseCredentialValidationResult> ValidateSftpCredentialsAsync(ClearingHouseDetailsModel clearingHouse)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new ClearinghouseCredentialValidationResult
            {
                ClearinghouseName = clearingHouse.Title,
                ClearinghouseId = clearingHouse.ClearingHouseId,
                ValidatedAt = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Validating SFTP credentials. ClearinghouseId={ClearinghouseId}, ClearinghouseName={ClearinghouseName}",
                clearingHouse.ClearingHouseId,
                clearingHouse.Title);

            ClearingHouseDetailsModel options = null;

            try
            {
                options = await _credentialResolver.ResolveAsync(clearingHouse);

                if (string.IsNullOrWhiteSpace(options.UrlLink))
                {
                    result.IsValid = false;
                    result.ErrorMessage = "SFTP host is not configured";
                    stopwatch.Stop();
                    result.DurationMs = stopwatch.ElapsedMilliseconds;

                    _logger.LogWarning(
                        "Configuration error: SFTP host missing. ClearinghouseId={ClearinghouseId}, ClearinghouseName={ClearinghouseName}",
                        clearingHouse.ClearingHouseId,
                        clearingHouse.Title);

                    return result;
                }

                if (string.IsNullOrWhiteSpace(options.UserName) || string.IsNullOrWhiteSpace(options.UserPassword))
                {
                    result.IsValid = false;
                    result.ErrorMessage = "SFTP credentials are not configured";
                    stopwatch.Stop();
                    result.DurationMs = stopwatch.ElapsedMilliseconds;

                    _logger.LogWarning(
                        "Configuration error: SFTP credentials missing. ClearinghouseId={ClearinghouseId}, ClearinghouseName={ClearinghouseName}",
                        clearingHouse.ClearingHouseId,
                        clearingHouse.Title);

                    return result;
                }

                _logger.LogInformation(
                    "ValidateSftpCredentialsAsync ClearingHouseId: {ClearingHouseId}, UrlLink: {UrlLink}, Port: {Port}, UserName: {UserName}",
                    clearingHouse.ClearingHouseId,
                    options.UrlLink,
                    options.Port,
                    options.UserName);

                using var client = new SftpClient(options.UrlLink, options.Port, options.UserName, options.UserPassword);
                var timeoutSetting = _configuration["SFTPConnectionTimeout"];
                int timeoutSeconds;
                if (!int.TryParse(timeoutSetting, out timeoutSeconds) || timeoutSeconds <= 0)
                {
                    timeoutSeconds = 180; // default timeout in seconds
                }
                client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

                _logger.LogDebug(
                    "Attempting SFTP connection. ClearinghouseId={ClearinghouseId}, Host={Host}:{Port}",
                    clearingHouse.ClearingHouseId,
                    options.UrlLink,
                    options.Port);

                client.Connect();

                if (!client.IsConnected)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Unable to establish SFTP connection";
                    stopwatch.Stop();
                    result.DurationMs = stopwatch.ElapsedMilliseconds;

                    _logger.LogError(
                        "SFTP connection failed. ClearinghouseId={ClearinghouseId}, ClearinghouseName={ClearinghouseName}",
                        clearingHouse.ClearingHouseId,
                        clearingHouse.Title);

                    return result;
                }


                result.IsValid = true;

                _logger.LogInformation(
                    "SFTP validation successful. ClearinghouseId={ClearinghouseId}, ClearinghouseName={ClearinghouseName}, Host={Host}:{Port}",
                    clearingHouse.ClearingHouseId,
                    clearingHouse.Title,
                    options.UrlLink,
                    options.Port);
                client.Disconnect();
            }
            catch (TimeoutException timeoutEx)
            {
                result.IsValid = false;
                result.ErrorMessage = "Connection timeout";

                _logger.LogError(
                    timeoutEx,
                    "Connection timeout. ClearinghouseId={ClearinghouseId}, ClearinghouseName={ClearinghouseName}",
                    clearingHouse.ClearingHouseId,
                    clearingHouse.Title);
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Unexpected error: {ex.Message}";

                _logger.LogError(
                    ex,
                    "Unexpected error during validation. ClearinghouseId={ClearinghouseId}, ClearinghouseName={ClearinghouseName}",
                    clearingHouse.ClearingHouseId,
                    clearingHouse.Title);
            }

            stopwatch.Stop();
            result.DurationMs = stopwatch.ElapsedMilliseconds;

            return result;
        }

        /// <summary>
        /// Validates SFTP credentials for all active clearinghouses defined in BillingClearingHousesEnum
        /// </summary>
        public async Task<List<ClearinghouseCredentialValidationResult>> ValidateMultipleClearinghousesAsync(List<ClearingHouseDetailsModel> clearinghouses)
        {
            if (clearinghouses == null || !clearinghouses.Any())
            {
                _logger.LogWarning("No clearinghouses provided for validation");
                return new List<ClearinghouseCredentialValidationResult>();
            }

            _logger.LogInformation(
                "Validating {Count} clearinghouses. Clearinghouses={ClearinghouseNames}",
                clearinghouses.Count,
                string.Join(", ", clearinghouses.Select(c => c.Title)));

            int maxConcurrency = DefaultMaxConcurrency;
            var maxConcurrencySetting = _configuration["SFTPValidationMaxConcurrency"];
            if (!string.IsNullOrWhiteSpace(maxConcurrencySetting) && int.TryParse(maxConcurrencySetting, out var parsedConcurrency) && parsedConcurrency > 0)
            {
                maxConcurrency = parsedConcurrency;
            }

            using var semaphore = new SemaphoreSlim(maxConcurrency);
            var validationTasks = clearinghouses.Select(async ch =>
            {
                await semaphore.WaitAsync();
                try
                {
                    return await ValidateSftpCredentialsAsync(ch);
                }
                finally
                {
                    semaphore.Release();
                }
            });
            var results = await Task.WhenAll(validationTasks);

            _logger.LogInformation(
                "Clearinghouse validation completed. Total={Total}, Success={Success}, Failed={Failed}",
                results.Length,
                results.Count(r => r.IsValid),
                results.Count(r => !r.IsValid));

            return results.ToList();
        }
    }
}
