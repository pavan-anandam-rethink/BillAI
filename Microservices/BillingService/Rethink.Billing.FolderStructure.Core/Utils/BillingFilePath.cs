using AutoMapper;
using Billing.FolderStructure.Core.Enum;
using Billing.FolderStructure.Core.Models;
using Billing.FolderStructure.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Services;
using System.Text;
using System.Text.RegularExpressions;

namespace Billing.FolderStructure.Core.Utils
{
    public class BillingFilePath : BaseService, IBillingFilePath
    {
        private readonly IRepository<BillingDbContext, ClaimSubmissionFunderSequenceEntity> _claimSubmissionFunderSequence;
        private readonly IRepository<BillingDbContext, ClaimSubmissionEntity> _claimSubmissionEntity;
        private readonly IRepository<BillingDbContext, ClaimEdiFilesEntity> _claimEdiFilesEntity;
        private readonly IBillingBlobService _billingBlobService;
        private readonly IMapper _mapper;
        private readonly ILogger<BillingFilePath> _logger;

        public BillingFilePath(IRepository<BillingDbContext, ClaimSubmissionFunderSequenceEntity> claimSubmissionFunderSequence,
            IRepository<BillingDbContext, ClaimSubmissionEntity> claimSubmissionEntity, IRepository<BillingDbContext, ClaimEdiFilesEntity> claimEdiFilesEntity,
            IBillingBlobService billingBlobService, IMapper mapper, ILogger<BillingFilePath> logger)
        {
            _claimSubmissionFunderSequence = claimSubmissionFunderSequence;
            _claimSubmissionEntity = claimSubmissionEntity;
            _claimEdiFilesEntity = claimEdiFilesEntity;
            _billingBlobService = billingBlobService;
            _mapper = mapper;
            _logger = logger;
        }


        public async Task<string> CreateFolderPath(BillingRequest billingRequest)
        {
            string date = DateTime.Now.ToString("yyyy'/'MM");
            var parts = billingRequest.FieldIdentifier.Split('/');
            string fileName = parts[^1];

            var ediFileType = billingRequest.Data != null && billingRequest.Data.Length > 0
                ? await GetEdiFileType(billingRequest) : "Unknown";

            ediFileType = ediFileType == "Unknown" ? parts[1] : ediFileType;

            var dirPath = BuildBaseFilePathForBillingBlob(billingRequest.BillingContainerName, ediFileType, billingRequest.AccountInfoId, billingRequest.FolderName, billingRequest.SubFolderName, fileName, date, billingRequest.ClearingHouseTitle);
            dirPath = NormalizePath(dirPath);

            string filePath = Regex.Replace(dirPath, "/{2,}", "/");

            return filePath;
        }

        public async Task<string> GetEdiFileType(BillingRequest billingRequest)
        {
            return await GetEdiFileTypeAsync(billingRequest.Data);
        }

        private string NormalizePath(string path)
        {
            if (path.StartsWith("/"))
                path = path.Substring(1, path.Length - 1);

            if (path.StartsWith("\\") || path.StartsWith("~/"))
            {
                path = path.Substring(2);
            }

            return path;
        }

        private string BuildBaseFilePathForBillingBlob(string? containerName, string ediFileType, int? accountInfoId, string folderName, string? subFolderName, string fileName, string date, string clearingHouseTitle)
        {
            var payeeName = accountInfoId.ToString();
            if (string.IsNullOrEmpty(payeeName) || payeeName == "0")
            {
                payeeName = "Global Error";
            }
            return $"{containerName}/{clearingHouseTitle}/{ediFileType}/{payeeName}/{folderName}/{subFolderName}/{date}/{fileName}";
        }

        private async Task<string> GetEdiFileTypeAsync(byte[] data)
        {
            if (data == null || data.Length == 0)
                return "Unknown";

            string content = Encoding.UTF8.GetString(data);

            // Extract file type from ST segment (e.g., 837, 835, 277)
            var match = Regex.Match(content, @"ST\*(\d{3})\*", RegexOptions.Compiled);

            return match.Success ? match.Groups[1].Value : "Unknown";
        }

        public async Task<(string containerName, string fullFilePath)> SplitFilePath(string filePath)
        {
            int index = filePath.IndexOf('/');

            if (index >= 0)
            {
                // '/' found, split the string
                return (filePath.Substring(0, index), filePath.Substring(index + 1));
            }
            else
            {
                // No '/' found — the entire string is the first part
                return (filePath, string.Empty);
            }
        }

        public async Task<TransactionControlNumberModel> GetTransactionControlNumber(string ediData)
        {
            string fileType = "Unknown";
            List<string?> claimIdentifiers = new List<string?>();
            List<int?> controlNumbers = new List<int?>();

            string? npiNumber = null;
            string? federalTaxId = null;

            ediData = ediData.Replace("\r", "").Replace("\n", "");
            var rawInterchanges = ediData.Split("ISA", StringSplitOptions.RemoveEmptyEntries);

            foreach (var rawPart in rawInterchanges)
            {
                var ediPart = "ISA" + rawPart;
                var segments = ediPart.Split('~', StringSplitOptions.RemoveEmptyEntries);

                if (segments.Length == 0)
                    continue;

                var stSegment = segments.FirstOrDefault(s => s.StartsWith("ST*"));
                if (stSegment == null)
                    continue;

                var stFields = stSegment.Split('*');
                if (stFields.Length < 3)
                    continue;

                fileType = stFields[1];
                var transactionType = stFields[1];

                // ------------------- 999 -------------------
                if (transactionType == ((int)FileTypes.Type999).ToString())
                {
                    var ak2Segment = segments.FirstOrDefault(s => s.StartsWith("AK2*"));
                    if (ak2Segment != null)
                    {
                        var ak2Fields = ak2Segment.Split('*');
                        if (ak2Fields.Length > 2 && int.TryParse(ak2Fields[2], out int ak2Ctrl))
                            controlNumbers.Add(ak2Ctrl);
                        else
                            controlNumbers.Add(null);
                    }
                }

                // ------------------- 277 -------------------
                else if (transactionType == ((int)FileTypes.Type277).ToString())
                {
                    for (int i = 0; i < segments.Length; i++)
                    {
                        if (segments[i].StartsWith("HL*2*"))
                        {
                            for (int j = i + 1; j < segments.Length; j++)
                            {
                                if (segments[j].StartsWith("HL*")) break;

                                if (segments[j].StartsWith("TRN*"))
                                {
                                    var trnFields = segments[j].Split('*');
                                    if (trnFields.Length > 2)
                                        claimIdentifiers.Add(trnFields[2]);
                                }
                            }
                            break;
                        }
                    }
                }

                // ------------------- 835 -------------------
                else if (transactionType == ((int)FileTypes.Type835).ToString())
                {
                    foreach (var segment in segments)
                    {
                        // Extract CLP
                        if (segment.StartsWith("CLP*"))
                        {
                            var clpFields = segment.Split('*');
                            if (clpFields.Length > 1)
                            {
                                claimIdentifiers.Add(clpFields[1]);

                                if (int.TryParse(stFields[2], out int stCtrl))
                                    controlNumbers.Add(stCtrl);
                                else
                                    controlNumbers.Add(null);
                            }
                        }

                        // NPI
                        if (segment.StartsWith("N1*"))
                        {
                            var n1 = segment.Split('*');
                            if (n1.Length >= 5 && n1[3] == "XX")
                                npiNumber = n1[4];
                        }

                        // Tax ID
                        if (segment.StartsWith("REF*"))
                        {
                            var refFields = segment.Split('*');
                            if (refFields.Length >= 3 && refFields[1] == "TJ")
                                federalTaxId = refFields[2];
                        }
                    }
                }

                // ------------------- 837 -------------------
                else if (transactionType == ((int)FileTypes.Type837).ToString())
                {
                    foreach (var segment in segments)
                    {
                        if (segment.StartsWith("CLM*"))
                        {
                            var clmFields = segment.Split('*');
                            if (clmFields.Length > 1)
                                claimIdentifiers.Add(clmFields[1].Trim());
                        }
                    }

                    if (int.TryParse(stFields[2], out int stCtrl))
                        controlNumbers.Add(stCtrl);
                    else
                        controlNumbers.Add(null);
                }

                else
                {
                    if (int.TryParse(stFields[2], out int stCtrl))
                        controlNumbers.Add(stCtrl);
                    else
                        controlNumbers.Add(null);
                }
            }

            return new TransactionControlNumberModel
            {
                FileType = fileType,
                NpiNumber = npiNumber,
                FederalTaxId = federalTaxId,
                ControlNumbers = controlNumbers.ToArray(),
                ClaimIdentifiers = claimIdentifiers.ToArray()
            };
        }


        public async Task<ClaimSubmissionEntity> FetchClaimSubmissionDataForERA(TransactionControlNumberModel model)
        {
            var controlNumbers = model.ControlNumbers ?? Array.Empty<int?>();
            var claimIdentifiers = model.ClaimIdentifiers ?? Array.Empty<string?>();
            var federalTaxId = model.FederalTaxId;

            string filetype = model.FileType;
            string? npiNumber = model.NpiNumber;

            // Clean the collections
            var controlNums = controlNumbers.Where(x => x.HasValue).Select(x => x!.Value).ToList();
            var claimIds = claimIdentifiers.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

            // Base query
            var query = _claimSubmissionEntity.Query().AsNoTracking()
                .Include(cs => cs.Claim);

            if (filetype == ((int)FileTypes.Type999).ToString() && controlNums.Any())
            {
                query.Where(x => controlNums.Contains(x.Id));
            }

            if (filetype != ((int)FileTypes.Type999).ToString() && claimIds.Any())
            {
                query.Where(x => claimIds.Contains(x.ClaimSubmissionIdentifier));
            }

            if (filetype == ((int)FileTypes.Type835).ToString())
            {
                query.Where(x => x.AccountFederalTaxId == federalTaxId || x.AccountNpiNumber == npiNumber);
            }

            var result = query.OrderByDescending(x => x.Id).ToList();
            var match = filetype == ((int)FileTypes.Type999).ToString()
                ? result.FirstOrDefault(x => x.Id == controlNums.First())
                : result.FirstOrDefault(x =>
                    x.ClaimSubmissionIdentifier != null &&
                    claimIds.Contains(x.ClaimSubmissionIdentifier.Trim())
                );

            return match;
        }

        public async Task<ClaimSubmissionEntity?> FetchClaimSubmissionDataForManualERA(TransactionControlNumberModel model, int accountInfoId)
        {
            // Extract and clean inputs
            var controlNumbers = model.ControlNumbers?.Where(x => x.HasValue).Select(x => x.Value).ToList()
                                 ?? new List<int>();

            var claimIdentifiers = model.ClaimIdentifiers?.Where(x => !string.IsNullOrWhiteSpace(x))
                                     .Select(x => x!.Trim()).ToList()
                                     ?? new List<string>();

            var federalTaxId = model.FederalTaxId;
            var npiNumber = model.NpiNumber;

            if (!claimIdentifiers.Any() && !controlNumbers.Any())
                return null;

            var query = _claimSubmissionEntity.Query()
                .AsNoTracking()
                .Include(cs => cs.Claim)
                .Where(x =>
                    (claimIdentifiers.Count == 0 || claimIdentifiers.Contains(x.ClaimSubmissionIdentifier)) &&
                    (string.IsNullOrEmpty(federalTaxId) || x.AccountFederalTaxId == federalTaxId || string.IsNullOrEmpty(npiNumber) || x.AccountNpiNumber == npiNumber)

                );

            var results = await query
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            // Return first matching claim submission
            return results
                .FirstOrDefault(x => x.ClaimSubmissionIdentifier != null &&
                                     claimIdentifiers.Contains(x.ClaimSubmissionIdentifier.Trim()));
        }

        /// <summary>
        /// Retrieves the EDI file content from Azure Blob Storage based on the provided search criteria.
        /// </summary>
        /// <param name="model">The model containing filter criteria such as AccountInfoId, ClaimSubmissionId, ClaimId, PaymentId, and FileType.</param>
        /// <returns>The EDI file content as a string, or <see cref="string.Empty"/> if no matching record or blob is found.</returns>
        public async Task<string> GetEdiFilesFromBlob(ClaimEdiFilesModel model)
        {
            try
            {
                _logger.LogInformation(
                    "{Service}.{Method} - Starting EDI file download. ClaimId={ClaimId}",
                    nameof(BillingFilePath), nameof(GetEdiFilesFromBlob), model.ClaimId);

                var query = _claimEdiFilesEntity.Query().AsNoTracking()
                    .Where(x => x.AccountInfoId == model.AccountInfoId && x.DateDeleted == null);

                if (model.PaymentId > 0)
                {
                    query = query.Where(x => x.PaymentId == model.PaymentId);
                }
                else
                {
                    if (!string.IsNullOrEmpty(model.BatchId))
                        query = query.Where(x => x.ClaimSubmission.ClaimSubmissionIdentifier == model.BatchId);

                    if (model.ClaimId > 0)
                        query = query.Where(x => x.ClaimId == model.ClaimId);
                }

                if (!string.IsNullOrEmpty(model.FileType))
                {
                    query = query.Where(x => x.FileType == model.FileType);
                }

                var data = await query
                    .Select(x => x.BlobFilePath)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(data))
                {
                    _logger.LogWarning(
                        "{Service}.{Method} - No matching EDI record found in DB. ClaimId={ClaimId}",
                        nameof(BillingFilePath), nameof(GetEdiFilesFromBlob), model.ClaimId);

                    return string.Empty;
                }

                // Retrieve the EDI file content from Azure Blob Storage using the obtained blob file path
                var ediStream = await _billingBlobService.DownloadBlobFromContainerAsync(BillingConstants.BillingContainerName, data);
                if (ediStream == null)
                {
                    _logger.LogWarning(
                        "{Service}.{Method} - Blob not found or empty. BlobPath={BlobPath}",
                        nameof(BillingFilePath), nameof(GetEdiFilesFromBlob), data);

                    return string.Empty;
                }

                // Reset position if stream supports seeking
                if (ediStream.CanSeek)
                    ediStream.Position = 0;

                using (var reader = new StreamReader(ediStream, Encoding.UTF8, true, 1024, leaveOpen: true))
                {
                    string ediContent = await reader.ReadToEndAsync();
                    _logger.LogInformation(
                        "{Service}.{Method} - Successfully read EDI content. Length={Length} characters",
                        nameof(BillingFilePath), nameof(GetEdiFilesFromBlob), ediContent?.Length ?? 0);

                    return ediContent;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "{Service}.{Method} - Error retrieving EDI file from blob. ClaimId={ClaimId}",
                    nameof(BillingFilePath), nameof(GetEdiFilesFromBlob), model.ClaimId);

                throw;
            }
        }

        /// <summary>
        /// Adds a new or updates an existing EDI file record in the ClaimEdiFiles table.
        /// Matches an existing record using AccountInfoId, ClaimSubmissionId, ClaimId, PaymentId, and FileType.
        /// If a matching record is found with the same FileType and ClaimId, it is updated; otherwise, a new record is created.
        /// </summary>
        /// <param name="model">The model containing the EDI file metadata and blob file path to persist.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task AddOrUpdateBlobFilePath(ClaimEdiFilesModel model)
        {
            try
            {
                _logger.LogInformation(
                   "{Service}.{Method} - Beginning EDI file blob path persistence. ClaimId={ClaimId}",
                   nameof(BillingFilePath), nameof(AddOrUpdateBlobFilePath), model.ClaimId);

                // Fetching existing record based on matching criteria
                var existingEntity = await _claimEdiFilesEntity.Query()
                 .Where(x => x.AccountInfoId == model.AccountInfoId
                             && (model.ClaimSubmissionId > 0 ? x.ClaimSubmissionId == model.ClaimSubmissionId : true)
                             && (model.ClaimId > 0 ? x.ClaimId == model.ClaimId : true)
                             && (model.PaymentId > 0 ? x.PaymentId == model.PaymentId : true)
                             && (!string.IsNullOrEmpty(model.FileType) ? x.FileType == model.FileType : true)
                             && x.DateDeleted == null)
                 .FirstOrDefaultAsync();

                // If an existing entity is found with the same FileType and ClaimId, update it; otherwise, create a new entity
                if (existingEntity != null && model.FileType?.Equals(existingEntity?.FileType, StringComparison.OrdinalIgnoreCase) == true
                    && model.ClaimId == existingEntity.ClaimId && model.ClaimSubmissionId == existingEntity.ClaimSubmissionId)
                {
                    _logger.LogInformation(
                        "{Service}.{Method} - Updating existing entity. ClaimId={ClaimId}, EntityId={EntityId}",
                        nameof(BillingFilePath), nameof(AddOrUpdateBlobFilePath), model.ClaimId, existingEntity.Id);

                    _mapper.Map(model, existingEntity);
                    MarkUpdated(existingEntity, model.MemberId);
                    _claimEdiFilesEntity.Update(existingEntity);
                }
                else
                {
                    _logger.LogInformation(
                        "{Service}.{Method} - Creating new entity. ClaimId={ClaimId}",
                        nameof(BillingFilePath), nameof(AddOrUpdateBlobFilePath), model.ClaimId);

                    var newEntity = _mapper.Map<ClaimEdiFilesEntity>(model);
                    MarkCreated(newEntity, model.MemberId);
                    await _claimEdiFilesEntity.AddAsync(newEntity);
                }

                await _claimEdiFilesEntity.CommitAsync();
                _logger.LogInformation(
                    "{Service}.{Method} - AddOrUpdate operation completed successfully. ClaimId={ClaimId}",
                    nameof(BillingFilePath), nameof(AddOrUpdateBlobFilePath), model.ClaimId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "{Service}.{Method} - Error adding or updating EDI file record. ClaimId={ClaimId}",
                    nameof(BillingFilePath), nameof(AddOrUpdateBlobFilePath), model.ClaimId);

                throw;
            }
        }
    }
}