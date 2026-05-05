using Azure.Storage.Blobs;
using Billing.FolderStructure.Core.Enum;
using Billing.FolderStructure.Core.Models;
using Billing.FolderStructure.Core.Utils;
using Microsoft.Extensions.Configuration;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Billing.FolderStructure.Core.Services
{
    public class BillingBlobService : IBillingBlobService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<BillingBlobService> _logger;
        public IConfiguration Configuration { get; }

        public BillingBlobService(BlobServiceClient blobServiceClient, ILogger<BillingBlobService> logger, IConfiguration configuration)
        {
            _blobServiceClient = blobServiceClient;
            _logger = logger;
            Configuration = configuration;
        }

        public async Task CreateBlobContainerAsync(CancellationToken cancellationToken = default)
        {
            var billingStorageConfig = new BillingStorageConfig();
            Configuration.GetSection("BillingStorageConfig").Bind(billingStorageConfig);

            var date = DateTime.UtcNow;
            string year = date.ToString("yyyy");
            string month = date.ToString("MM");

            var containerClient =
                _blobServiceClient.GetBlobContainerClient(billingStorageConfig.ContainerName);

            _logger.LogInformation("Attempting to create container: {ContainerName}", billingStorageConfig.ContainerName);
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            _logger.LogInformation("Container '{ContainerName}' created or already exists.", billingStorageConfig.ContainerName);

            foreach (var source in billingStorageConfig.Sources)
            {
                string sourceName = source.Key;              // Availity / Stedy
                var transactionTypes = source.Value;         // 835, 837, 270, etc

                foreach (var mainFolder in transactionTypes)
                {
                    foreach (var clinic in billingStorageConfig.Accounts)
                    {
                        var foldersToCreate =
                            GetSubFolders(mainFolder, billingStorageConfig.FolderStructure);

                        foreach (var folderPath in foldersToCreate)
                        {
                            string basePath =
                                $"{sourceName}/{mainFolder}/{clinic}";

                            if (!string.IsNullOrWhiteSpace(folderPath))
                            {
                                basePath =
                                    $"{basePath}/{folderPath}/{year}/{month}/3";
                            }

                            try
                            {
                                var blobClient = containerClient.GetBlobClient(basePath);

                                await blobClient.UploadAsync(
                                    BinaryData.FromString(string.Empty),
                                    overwrite: true,
                                    cancellationToken);

                                _logger.LogInformation("Uploaded blob on azure {Container}/{BlobName}", containerClient.Name, blobClient.Name);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(
                                    ex,
                                    "Failed to upload blob {BlobPath} on Azure (Source={Source}, Transaction={Transaction}, Clinic={Clinic})",
                                    basePath,
                                    sourceName,
                                    mainFolder,
                                    clinic);
                            }
                        }
                    }
                }
            }

            _logger.LogInformation("Blob folder structure creation completed.");
        }

        private string[] GetSubFolders(string mainFolder, Dictionary<string, string[]> subFolder)
        {
            if (subFolder == null || subFolder.Count == 0)
                return Array.Empty<string>();

            if (subFolder.ContainsKey(mainFolder))
                return subFolder[mainFolder];

            return Array.Empty<string>();
        }

        public async Task<string> UploadIntoContainerAsync(string containerName, string blobPath, MemoryStream fileStream)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            string directoryPath = Path.GetDirectoryName(blobPath).Replace("\\", "/");
            string fileName = Path.GetFileNameWithoutExtension(blobPath);
            string extension = Path.GetExtension(blobPath);
            string transactionType = blobPath.Split('/')[1];

            int counter = 1;
            string newBlobPath = blobPath;
            var blobClient = containerClient.GetBlobClient(newBlobPath);
            string uniqueName = string.Empty;

            while (await blobClient.ExistsAsync() && (transactionType == ((int)FileTypes.Type835).ToString()
                                                       || transactionType == ((int)FileTypes.Type270).ToString()
                                                       || transactionType == ((int)FileTypes.Type271).ToString()))
            {
                uniqueName = $"{fileName}_{counter}{extension}";
                newBlobPath = $"{directoryPath}/{uniqueName}";  // folder path + updated file name
                blobClient = containerClient.GetBlobClient(newBlobPath);
                counter++;
            }

            // Reset stream position before upload
            fileStream.Position = 0;

            // Upload (overwrite is false by default if blob is new)
            var result = await blobClient.UploadAsync(fileStream, overwrite: true);
            return string.IsNullOrEmpty(uniqueName) ? $"{fileName}{extension}" : uniqueName;
        }

        public async Task<MemoryStream> DownloadBlobFromContainerAsync(string containerName, string filePath)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName.ToLowerInvariant());
            var fileClient = containerClient.GetBlobClient(filePath);
            var fileResponse = await fileClient.DownloadAsync();

            var downloadFileStream = new MemoryStream();
            await fileResponse.Value.Content.CopyToAsync(downloadFileStream);

            return downloadFileStream;
        }

        public async Task DeleteContainerAsync(string containerName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var result = await containerClient.DeleteIfExistsAsync();
        }


        public async Task DeleteBlobFromContainerAsync(string containerName, string filePath)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var fileClient = containerClient.GetBlobClient(filePath);

            var result = await fileClient.DeleteIfExistsAsync();
        }


        public async Task Update999ReportAsync(string containerName, EDI999Summary summary, string filePath)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            var parts = filePath.Split('/');
            var today = DateTime.Today.ToString("yyyyMMdd");
            parts[^1] = today + ".txt";
            filePath = string.Join("/", parts);
            var blobClient = containerClient.GetBlobClient(filePath);

            string separator = new string('-', 112);
            string reportHeader = $@"Rethink Billing:
            {separator}
            EDI 999 Daily Parsing Report
            Date: {DateTime.UtcNow:yyyy-MM-dd}
            {separator}
            Total 999 Files Processed: {{0}}
            Summary by File:
            {separator}
            {FormatReportHeader()}
            {separator}";

            string reportFooter = $@"
            {separator}
            Total Transaction Sets: {{0}}
            Accepted: {{1}}
            Rejected: {{2}}
            Partial: {{3}}
            {separator}
            Notes:
            - 'Partial' means some transaction sets accepted, some rejected.
            - Follow-up required on rejected sets.
            {separator}";

            string existingContent = string.Empty;

            if (await blobClient.ExistsAsync())
            {
                var download = await blobClient.DownloadContentAsync();
                existingContent = download.Value.Content.ToString();
            }

            List<string> lines = new();

            if (!string.IsNullOrWhiteSpace(existingContent))
            {
                var allLines = existingContent
                    .Replace("\r\n", "\n")
                    .Replace("\r", "\n")
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.TrimEnd('\r'))
                    .ToList();

                int headerIndex = allLines.FindIndex(l => l.Trim().StartsWith("File Name", StringComparison.OrdinalIgnoreCase));
                if (headerIndex >= 0 && allLines.Count > headerIndex + 2)
                {
                    int dataStartIndex = headerIndex + 2;
                    int summaryEndIndex = allLines.FindIndex(dataStartIndex, l => l.Trim().StartsWith("----") && l.Trim().All(c => c == '-' || c == ' '));
                    if (summaryEndIndex == -1)
                        summaryEndIndex = allLines.Count;

                    lines = allLines.Skip(dataStartIndex).Take(summaryEndIndex - dataStartIndex)
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .ToList();
                }
            }

            string newLine = FormatReportLine(summary);

            lines.Add(newLine);

            int totalFilesProcessed = lines.Count;
            int totalTransactionSets = 0, totalAccepted = 0, totalRejected = 0, totalPartial = 0;

            foreach (var line in lines)
            {
                var lineParts = line.Split('|').Select(p => p.Trim()).ToArray();
                if (lineParts.Length >= 7)
                {
                    int.TryParse(lineParts[2], out int txSets);
                    int.TryParse(lineParts[3], out int accepted);
                    int.TryParse(lineParts[4], out int rejected);
                    int.TryParse(lineParts[5], out int partial);

                    totalTransactionSets += txSets;
                    totalAccepted += accepted;
                    totalRejected += rejected;
                    totalPartial += partial;
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine(string.Format(reportHeader, totalFilesProcessed));
            foreach (var line in lines.OrderBy(x => x))
                sb.AppendLine(line);
            sb.AppendLine(string.Format(reportFooter,
                totalTransactionSets,
                totalAccepted,
                totalRejected,
                totalPartial));

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
            await blobClient.UploadAsync(stream, overwrite: true);
        }

        private string FormatReportHeader()
        {
            return
                "File Name".PadRight(22) + " | " +
                "Partner".PadRight(22) + " | " +
                "Total TX Sets".PadRight(15) + " | " +
                "Accepted".PadRight(8) + " | " +
                "Rejected".PadRight(8) + " | " +
                "Partial".PadRight(7) + " | " +
                "Status".PadRight(8);
        }

        private string FormatReportLine(EDI999Summary summary)
        {
            return
                (summary.FileName?.Trim() ?? "").PadRight(22) + " | " +
                (summary.Partner?.Trim() ?? "").PadRight(22) + " | " +
                summary.TotalTransactionSets.ToString().PadRight(15) + " | " +
                summary.Accepted.ToString().PadRight(8) + " | " +
                summary.Rejected.ToString().PadRight(8) + " | " +
                summary.Partial.ToString().PadRight(7) + " | " +
                (summary.Status?.Trim() ?? "").PadRight(8);
        }

        public async Task Update277DailySummaryReportAsync(string containerName, string ediContent, string reportFileName, List<string> existingClaimIds)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            var totalClaims = 0; var accepted = 0; var rejected = 0;
            var report = EDI277DetailedReportReader.Parse(ediContent);
            var claims = report.Claims.ToList();

            var parts = reportFileName.Split('/');
            var today = DateTime.Today.ToString("yyyyMMdd");
            parts[^1] = today + ".txt";
            reportFileName = string.Join("/", parts);

            var blobClient = containerClient.GetBlobClient(reportFileName);

            string reportHeader = "Rethink Billing:\nDate: " + today;

            int currentTotalClaims = 0, currentAccepted = 0, currentRejected = 0;

            if (await blobClient.ExistsAsync())
            {
                var download = await blobClient.DownloadContentAsync();
                var existingContent = download.Value.Content.ToString();

                foreach (var line in existingContent.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (line.StartsWith("Total Claims:"))
                        int.TryParse(line.Split(':')[1].Trim(), out currentTotalClaims);
                    else if (line.StartsWith("Accepted:"))
                        int.TryParse(line.Split(':')[1].Trim(), out currentAccepted);
                    else if (line.StartsWith("Rejected:"))
                        int.TryParse(line.Split(':')[1].Trim(), out currentRejected);
                }
            }

            foreach(var claim in claims)
            {
                if (!existingClaimIds.Contains(claim.ClaimTrnNumber))
                {
                    if(claim.Status == "Accepted")
                        accepted++;
                    else if (claim.Status == "Rejected")
                        rejected++;

                    totalClaims++;
                }
            }
            currentTotalClaims += totalClaims;
            currentAccepted += accepted;
            currentRejected += rejected;

            var sb = new StringBuilder();
            sb.AppendLine(reportHeader);
            sb.AppendLine($"Total Claims: {currentTotalClaims}");
            sb.AppendLine($"Accepted: {currentAccepted}");
            sb.AppendLine($"Rejected: {currentRejected}");

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
            await blobClient.UploadAsync(stream, overwrite: true);
        }

        public async Task<List<string>> Update277DetailedReportAsync(string containerName, string ediData, string fullFileName)
        {
            var parts = fullFileName.Split('/');
            string fileName = parts[^1];
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            var today = DateTime.Today.ToString("yyyyMMdd");
            parts[^1] = today + ".txt";
            fullFileName = string.Join("/", parts);
            var blobClient = containerClient.GetBlobClient(fullFileName);

            var report = EDI277DetailedReportReader.Parse(ediData);

            StringBuilder sb = new StringBuilder();
            var existingClaimIds = new List<string>();

            if (await blobClient.ExistsAsync())
            {
                var existing = await blobClient.DownloadContentAsync();
                var claimIds = new List<string>();
                report.Claims.Where(x => x.ClaimTrnNumber != null).ToList().ForEach(c => claimIds.Add(c.ClaimTrnNumber));
                foreach (var claim in claimIds)
                { 
                    if (System.Text.Encoding.UTF8.GetString(existing.Value.Content.ToArray()).Contains(fileName) || System.Text.Encoding.UTF8.GetString(existing.Value.Content.ToArray()).Contains(claim))
                    {
                        existingClaimIds.Add(claim);
                    }
                }
                sb.Append(existing.Value.Content.ToString());
            }

            var renderedSummary = EDI277DetailedReportRenderer.Render(report, existingClaimIds);
            if (!string.IsNullOrEmpty(renderedSummary)) 
            {
                sb.AppendLine("Rethink Billing:");
                sb.AppendLine(new string('=', 58));
                sb.AppendLine($"File: {fileName}");
                sb.AppendLine($"Sender: {(string.IsNullOrWhiteSpace(report.Sender) ? "UnknownSender" : report.Sender)}");
                sb.AppendLine($"Receiver: {(string.IsNullOrWhiteSpace(report.Receiver) ? "UnknownReceiver" : report.Receiver)}");
                sb.AppendLine($"Date: {report.ReportDate:yyyy-MM-dd}");
                sb.AppendLine(new string('-', 58));
                sb.AppendLine(renderedSummary.Trim());
                sb.AppendLine();
            }

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
            await blobClient.UploadAsync(stream, overwrite: true);
            return existingClaimIds;
        }

        public async Task<string> UploadAvailityFilesToBlobBackupAsync(string containerName, string blobPath, MemoryStream fileStream)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            string directoryPath = Path.GetDirectoryName(blobPath).Replace("\\", "/");
            string fileName = Path.GetFileNameWithoutExtension(blobPath);
            string extension = Path.GetExtension(blobPath);
            string transactionType = blobPath.Split('/')[0];

            int counter = 1;
            string newBlobPath = blobPath;
            var blobClient = containerClient.GetBlobClient(newBlobPath);
            string uniqueName = string.Empty;

            // Reset stream position before upload
            fileStream.Position = 0;

            // Upload (overwrite is false by default if blob is new)
            var result = await blobClient.UploadAsync(fileStream, overwrite: true);
            return uniqueName;
        }
    }
}