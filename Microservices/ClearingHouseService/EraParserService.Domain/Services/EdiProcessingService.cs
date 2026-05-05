using Billing.FolderStructure.Core.Enum;
using Billing.FolderStructure.Core.Models;
using Billing.FolderStructure.Core.Services;
using Billing.FolderStructure.Core.Utils;
using BillingService.Domain.Interfaces.Files;
using EdiFabric.Core.Model.Edi;
using EdiFabric.Core.Model.Edi.ErrorContexts;
using EdiFabric.Framework;
using EdiFabric.Framework.Readers;
using EdiFabric.Templates.Common;
using EdiFabric.Templates.Hipaa5010;
using EdiFabric.Templates.Hipaa5010_999;
using EraParserService.Domain.Services.EdiExtensionParsers;
using EraParserService.Domain.Services.EdiParsers.Edi277;
using EraParserService.Domain.Services.EdiParsers.Edi835;
using EraParserService.Domain.Services.EdiParsers.Edi999;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Handlers;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Messaging;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Services;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Thon.Hotels.FishBus;

namespace EraParserService.Domain.Services
{
    public class EdiProcessingService : BaseService, IEdiProcessingService
    {
        private readonly IBillingBlobService _billingBlobService;
        private readonly IRepository<BillingDbContext, PaymentEraUploadEntity> _paymentEraUploadRepository;
        private readonly ILoggerFactory _loggerFactory;
        private readonly BillingDbContext _dbContext;
        private readonly IBlobProcessingService _blobProcessingService;
        private readonly IEraValidationService _validationService;
        private readonly IPaymentService _paymentService;
        private readonly IClaimAckParser _claimAckParser;
        private readonly IEdi999Parser _edi999Parser;
        private readonly IClaimsSummaryDataParser _claimsSummaryDataParser;
        private readonly ILogger _logger;
        public EraParserError LastError { get; private set; }
        private readonly IConfiguration _configuration;
        private readonly string _ApiUrl;
        private readonly string _XApiKey;
        private readonly IMessageBus _messageBus;
        private readonly IBillingFilePath _billingFilePath;
        private readonly IRepository<BillingDbContext, ClaimSubmissionFunderSequenceEntity> _claimSubmissionFunderSequence;
        private readonly IRepository<BillingDbContext, ClaimSubmissionEntity> _claimSubmissionEntity;
        private readonly string _senderId;
        private readonly string _receiverId;

        private string ContainerName = "eraerrorfiles";
        private readonly IEdiFilesDownload _ediFilesDownload;

        public EdiProcessingService(IBillingBlobService billingBlobService,
                                    IRepository<BillingDbContext, PaymentEraUploadEntity> paymentEraUploadRepository,
                                    ILoggerFactory loggerFactory,
                                    BillingDbContext dbContext,
                                    IBlobProcessingService blobProcessingService,
                                    IEraValidationService validationService,
                                    IPaymentService paymentService,
                                    IClaimAckParser claimAckParser,
                                    IEdi999Parser edi999Parser,
                                    IClaimsSummaryDataParser claimsSummaryDataParser,
                                    IConfiguration configuration,
                                    IMessageBus messageBus,
                                    IBillingFilePath billingFilePath,
                                    IRepository<BillingDbContext, ClaimSubmissionFunderSequenceEntity> claimSubmissionFunderSequence,
                                    IKeyVaultProviderService keyVaultProviderService,
                                    IRepository<BillingDbContext, ClaimSubmissionEntity> claimSubmissionEntity,
                                    IEdiFilesDownload ediFilesDownload
                                    )
        {
            _billingBlobService = billingBlobService;
            _paymentEraUploadRepository = paymentEraUploadRepository;
            _loggerFactory = loggerFactory;
            _dbContext = dbContext;
            _blobProcessingService = blobProcessingService;
            _validationService = validationService;
            _paymentService = paymentService;
            _claimAckParser = claimAckParser;
            _edi999Parser = edi999Parser;
            _claimsSummaryDataParser = claimsSummaryDataParser;
            _logger = _loggerFactory.CreateLogger(GetType());
            _configuration = configuration;
            _ApiUrl = keyVaultProviderService.GetSecretAsync(Convert.ToString(_configuration.GetSection("BillingApiUrl").Value)).Result;
            _XApiKey = keyVaultProviderService.GetSecretAsync(Convert.ToString(_configuration.GetSection("BillingApiKey").Value)).Result;
            _messageBus = messageBus;
            _validationService = validationService;
            _billingFilePath = billingFilePath;
            _claimSubmissionFunderSequence = claimSubmissionFunderSequence;
            _claimSubmissionEntity = claimSubmissionEntity;
            _receiverId = configuration["EdiSettings:SubmitterRethinkId"];
            _senderId = configuration["EdiSettings:BillerRethinkId"];
            _ediFilesDownload = ediFilesDownload;
        }

        public async Task<HandlerResult> ProcessFile(EdiDownloadData downloadData, Stream ediStream)
        {
            // (CF) Claims Summary Data File
            var isEdiCFExtensionFile = downloadData.FileIdentifier.StartsWith("CF");

            if (isEdiCFExtensionFile)
            {
                return await ProcessEdiExtension(downloadData, ediStream);
            }
            else
            {
                return await ProcessEdi(downloadData, ediStream);
            }
        }

        public async Task<HandlerResult> ProcessFile(EdiDownloadData downloadData)
        {
            var (containerName, fullFilePath) = await _billingFilePath.SplitFilePath(downloadData.FileIdentifier);
            //var ediStream = await _blobProcessingService.DownloadBlobFromContainerAsync(downloadData.ContainerName, downloadData.FileIdentifier);
            var ediStream = await _billingBlobService.DownloadBlobFromContainerAsync(BillingConstants.BillingContainerName, fullFilePath);

            ediStream.Seek(0, SeekOrigin.Begin);
            return await ProcessFile(downloadData, ediStream);
        }

        private async Task<HandlerResult> ProcessEdi(EdiDownloadData downloadData, Stream ediStream)
        {
            using var memoryStream = ediStream;

            memoryStream.Position = 0;
            using (var sr = new StreamReader(memoryStream, Encoding.UTF8, true, 1024, true))
            {
                var firstLine = sr.ReadLine();
                var isaSegment = firstLine?.Split('*');

                if (isaSegment != null && isaSegment[0] == "ISA" && isaSegment.Length >= 17)
                {
                    var componentSeparator = isaSegment[16][0];
                    var separators = Separators.X12;
                    separators.ComponentDataElement = componentSeparator;
                    separators.Segment = isaSegment[16][1];
                    memoryStream.Position = 0;

                    var settings = new X12ReaderSettings
                    {
                        Separators = separators,
                        NoEnvelope = true,
                        ContinueOnError = true
                    };

                    List<IEdiItem> ediItems;
                    using (var ediReader = new X12Reader(ediStream, EdiHelper.LoadFactory, settings))
                    {
                        ediItems = ediReader.ReadToEnd().ToList();
                        var readerErrors = ediItems.OfType<ReaderErrorContext>();

                        if (readerErrors.Any())
                        {
                            var readerException = readerErrors.First().Exception;
                            throw new Exception(readerException.Message, readerException);
                        }

                        var cId = string.Empty;
                        var ediText = ediStream.LoadToString();
                        var isEraType = ediItems.OfType<TS835>().Any();
                        if (isEraType)
                        {
                            // Extract ISA06 and ISA08
                            var IsaSegment = ediText.Split('~')
                                                    .FirstOrDefault(x => x.StartsWith("ISA"));

                            string senderId = string.Empty;
                            string receiverId = string.Empty;

                            if (IsaSegment != null)
                            {
                                var parts = IsaSegment.Split('*');

                                if (parts.Length >= 9)
                                {
                                    senderId = parts[6].Trim();  // ISA06
                                    receiverId = parts[8].Trim(); // ISA08
                                }
                            }

                            var checkNumberPattern = new Regex(@"TRN\*1\*([^\*~]+)");
                            var checkMatch = checkNumberPattern.Match(ediText);

                            // Extract check/EFT number
                            string checkNumber = checkMatch.Success ? checkMatch.Groups[1].Value : string.Empty;

                            // Validate payment type and check number
                            if (senderId == _senderId && receiverId == _receiverId && !string.IsNullOrEmpty(checkNumber))
                            {
                                var existingPayment = await _dbContext.Payments
                                    .FirstOrDefaultAsync(x => x.ReferenceNumber == checkNumber && x.AccountInfoId == downloadData.AccountInfoId && x.DateDeleted == null);

                                if (existingPayment != null && !existingPayment.IsManualPayment)
                                {
                                    var parts = downloadData.FileIdentifier.Split('/');
                                    string fileName = parts[^1];
                                    var billingBlobModel = new BillingFolderStructureModel
                                    {
                                        Data = System.Text.Encoding.UTF8.GetBytes(ediText),
                                        FileName = fileName,
                                        ClearingHouseId = downloadData.ClearingHouseId
                                    };
                                    billingBlobModel.Message = ($"Payment with check/reference number {checkNumber} already exists.");
                                    await UploadIntoEraBillingBlob(billingBlobModel, BlobFolderNames.Duplicate, BlobFolderNames.ErrorLogs);

                                    return HandlerResult.Failed();
                                }
                            }

                            _logger.LogInformation($"EdiProcessorService.ProcessEra [{downloadData.DownloadDateTime:G}]: {downloadData.FileIdentifier}");
                            var claimIdentifiers = ediItems.OfType<TS835>()
                                .SelectMany(x => x.Loop2000)
                                .SelectMany(x => x.Loop2100)
                                .Select(x => x.CLP_ClaimPaymentInformation?.PatientControlNumber_01)
                                .ToList();
                            #region RHD-3297-Production Issue - Payment Posting: EOB Details Screen Missing Claims Not Present in System
                            //var result = ediItems.OfType<TS835>()
                            //    .Select(x => x.AllN1)
                            //    .Select(x => x.Loop1000B)
                            //    .Select(x => new
                            //    {
                            //        fedralTaxId = x.REF_PayeeAdditionalIdentification
                            //            .Where(refId => refId.ReferenceIdentificationQualifier_01 == "TJ")
                            //            .Select(refId => refId.ReferenceIdentificationREF_02)
                            //            .FirstOrDefault(),

                            //        npiNumber = (x.N1_PayeeIdentification != null && x.N1_PayeeIdentification.EntityIdentifierCode_01 == "PE")
                            //            ? x.N1_PayeeIdentification.IntermediaryBankIdentifier_04
                            //            : null
                            //    });

                            //int invalidClaims = 0;
                            //foreach (var ci in claimIdentifiers)
                            //{
                            //    var taxId = result != null ? result.Select(r => r.fedralTaxId).FirstOrDefault() : null;
                            //    var npiNumber = result != null ? result.Select(r => r.npiNumber).FirstOrDefault() : null;
                            //    cId = ci;
                            //    var claimSubmission = _dbContext.ClaimSubmissions
                            //         .Where(cs => cs.ClaimSubmissionIdentifier == ci &&
                            //                      (cs.AccountFederalTaxId == taxId ||
                            //                      cs.AccountNpiNumber == npiNumber))
                            //         .Select(cs => cs.Claim)
                            //         .FirstOrDefault();


                            //    if (claimSubmission == null || claimSubmission.AccountInfoId == 0)
                            //    {
                            //        await UploadErrorDueToMissingAccountInfoId(downloadData.FileIdentifier, ci, ediText);
                            //        // if invalid claim identifier found, then logs the error and process next claim
                            //        //return HandlerResult.Failed();
                            //        invalidClaims++;
                            //        continue;
                            //    }

                            //    downloadData.AccountInfoId = claimSubmission.AccountInfoId;
                            //}
                            //if (invalidClaims != claimIdentifiers.Count())
                            #endregion
                            return await ProcessEra(downloadData, ediItems, ediText, downloadData.AccountInfoId, downloadData.PaymentEraUploadId ?? 0, cId);
                        }

                        var is277Type = ediItems.OfType<TS277A>().Any();
                        if (is277Type)
                        {
                            _logger.LogInformation($"EdiProcessorService.Process277 [{downloadData.DownloadDateTime:G}]: {downloadData.FileIdentifier}");
                            return await Process277(downloadData, ediItems, ediText);
                        }

                        var is999Type = ediItems.OfType<TS999>().Any();
                        if (is999Type)
                        {
                            _logger.LogInformation($"EdiProcessorService.Process999 [{downloadData.DownloadDateTime:G}]: {downloadData.FileIdentifier}");
                            return await Process999(downloadData, ediItems, ediText);
                        }
                    }

                }
            }

            return HandlerResult.Failed();
        }

        private async Task UploadIntoEraBillingBlob(BillingFolderStructureModel model, BlobFolderNames folderName, BlobFolderNames? subFolderName)
        {
            var dataString = System.Text.Encoding.UTF8.GetString(model.Data);
            var transactionControlResult = await _billingFilePath.GetTransactionControlNumber(dataString);
            var result = await _billingFilePath.FetchClaimSubmissionDataForERA(transactionControlResult);
            int accountInfoId = result != null
                    ? result.Claim.AccountInfoId
                    : 0;

            var billingRequest = new BillingRequest
            {
                FieldIdentifier = model.FileName,
                FolderName = folderName.ToString(),
                AccountInfoId = accountInfoId,
                Data = model.Data,
                BillingContainerName = BillingConstants.BillingContainerName,
                TransactionNumber = result?.Id,
                SubFolderName = null,
                ClearingHouseTitle = Enum.GetName(typeof(BillingClearingHousesEnum), model.ClearingHouseId)
            };

            var billingFilePath = await _billingFilePath.CreateFolderPath(billingRequest);
            var (containerName, fullFilePath) = await _billingFilePath.SplitFilePath(billingFilePath);
            var modelData = model.Data;
            if (subFolderName != null) model.Data = Encoding.UTF8.GetBytes(model.Message);
            await _billingBlobService.UploadIntoContainerAsync(BillingConstants.BillingContainerName, fullFilePath, new MemoryStream(model.Data));
            model.Data = modelData;

            billingRequest.FolderName = BlobFolderNames.Incoming.ToString();

            billingFilePath = await _billingFilePath.CreateFolderPath(billingRequest);
            (containerName, fullFilePath) = await _billingFilePath.SplitFilePath(billingFilePath);
            await _billingBlobService.DeleteBlobFromContainerAsync(BillingConstants.BillingContainerName, fullFilePath);
        }

        private async Task<HandlerResult> ProcessEdiExtension(EdiDownloadData downloadData, Stream ediStream)
        {
            try
            {
                await _claimsSummaryDataParser.ParseAsync(downloadData.AccountInfoId, downloadData.FileIdentifier, ediStream.LoadToString());
            }
            catch (Exception ex)
            {
                LogError($"Error parsing extension document - skipping: fileID={downloadData.FileIdentifier} Error={ex.Message}", ex);
                return HandlerResult.Failed();
            }

            return HandlerResult.Success();
        }

        private async Task<HandlerResult> ProcessEra(EdiDownloadData downloadData,
                                                    List<IEdiItem> ediItems,
                                                    string ediText,
                                                    int accountInfoId,
                                                    int paymentEraId,
                                                    string cId)
        {
            var parts = downloadData.FileIdentifier.Split('/');
            string fileName = parts[^1];

            var billingBlobModel = new BillingFolderStructureModel
            {
                Data = System.Text.Encoding.UTF8.GetBytes(ediText),
                FileName = fileName,
                AccountInfoId = accountInfoId,
                ClearingHouseId = downloadData.ClearingHouseId
            };

            var xmlTransactions = new List<string>();
            var eraPayments = new List<PaymentEntity>();
            try
            {
                var parser = new EraParser(_logger, _paymentService, _dbContext, downloadData.AccountInfoId, downloadData.PaymentEraUploadId, fileName, ediItems, ediText, this);
                eraPayments = await parser.ParseDocument(billingBlobModel, cId);
                await SaveDocuments(eraPayments);
                billingBlobModel.PaymentId = eraPayments.Select(x => x.Id).FirstOrDefault();
                await UploadIntoBillingBlob(billingBlobModel, BlobFolderNames.Processing, null);

                try
                {
                    await _validationService.ValidateEraPayments(downloadData.AccountInfoId, fileName, eraPayments);
                    billingBlobModel.Message = ($"File {fileName} Succesfully processed.");
                    await UploadIntoBillingBlob(billingBlobModel, BlobFolderNames.Logs, BlobFolderNames.ProcessingLogs);

                }
                catch (Exception ex)
                {
                    billingBlobModel.PaymentId = eraPayments.Select(x => x.Id).FirstOrDefault();
                    LogError($"Error validating document - fileID={fileName} Error={ex.Message}", ex);
                    await UploadIntoBillingBlob(billingBlobModel, BlobFolderNames.Failed, null);
                    billingBlobModel.Message = ($"Error validating document - for ClaimIdentifier '{cId}' Error={ex.Message}\nTimestamp: {DateTime.UtcNow:G}");
                    await UploadIntoBillingBlob(billingBlobModel, BlobFolderNames.Logs, BlobFolderNames.ErrorLogs);

                    return HandlerResult.Failed();
                }
            }
            catch (Exception ex)
            {
                LogError($"Error parsing 835 document - skipping: fileID={fileName} Error={ex.Message}", ex);
                //TODO: save and report document error - requires that we know the AccountInfoId prior to parsing
                await UploadIntoBillingBlob(billingBlobModel, BlobFolderNames.Failed, null);
                billingBlobModel.Message = ($"Error parsing 835 document - for ClaimIdentifier = '{cId}' fileName = '{fileName}' Error={ex.Message}\nTimestamp: {DateTime.UtcNow:G},\n {ex.InnerException?.Message}");

                await UploadIntoBillingBlob(billingBlobModel, BlobFolderNames.Logs, BlobFolderNames.ErrorLogs);

                return HandlerResult.Failed();
            }

            //await UploadErrorsToBlobStorage(accountInfoId, fileID, eraPayments);

            await UploadIntoBillingBlob(billingBlobModel, BlobFolderNames.Archive, null);

            await SendMessageToTopic(eraPayments);
            return HandlerResult.Success();
        }

        public async Task uploadToBilling(BillingFolderStructureModel model, BlobFolderNames folderName, BlobFolderNames? subFolderName)
        {
            if (folderName == BlobFolderNames.Failed)
            {
                throw new FileLoadException("File data is not correct");
            }
        }

        private async Task UploadIntoBillingBlob(BillingFolderStructureModel model, BlobFolderNames folderName, BlobFolderNames? subFolderName)
        {
            var dataString = System.Text.Encoding.UTF8.GetString(model.Data);
            var transactionControlResult = await _billingFilePath.GetTransactionControlNumber(dataString);
            var result = await _billingFilePath.FetchClaimSubmissionDataForERA(transactionControlResult);
            int accountInfoId = result != null
                    ? result.Claim.AccountInfoId
                    : (int)model.AccountInfoId;

            var billingRequest = new BillingRequest
            {
                FieldIdentifier = model.FileName,
                FolderName = folderName.ToString(),
                AccountInfoId = accountInfoId,
                Data = model.Data,
                BillingContainerName = BillingConstants.BillingContainerName,
                TransactionNumber = result?.Id,
                SubFolderName = subFolderName.ToString(),
                ClearingHouseTitle = Enum.GetName(typeof(BillingClearingHousesEnum), model.ClearingHouseId),
                ClearingHouseId = model.ClearingHouseId,
                PaymentId = model.PaymentId
            };

            var billingFilePath = await _billingFilePath.CreateFolderPath(billingRequest);
            var (containerName, fullFilePath) = await _billingFilePath.SplitFilePath(billingFilePath);
            var modelData = model.Data;
            if (subFolderName != null)
            {
                model.Data = Encoding.UTF8.GetBytes(model.Message);
            }
            else
            {
                await _ediFilesDownload.SaveClaimEdiFilePath(billingRequest, fullFilePath, result);
            }
            await _billingBlobService.UploadIntoContainerAsync(BillingConstants.BillingContainerName, fullFilePath, new MemoryStream(model.Data));
            model.Data = modelData;
            if (string.IsNullOrEmpty(billingRequest.SubFolderName))
            {
                if (billingRequest.FolderName == "Processing")
                {
                    billingRequest.FolderName = BlobFolderNames.Incoming.ToString();
                }
                else
                {
                    billingRequest.FolderName = BlobFolderNames.Processing.ToString();
                }
                billingFilePath = await _billingFilePath.CreateFolderPath(billingRequest);
                (containerName, fullFilePath) = await _billingFilePath.SplitFilePath(billingFilePath);
                await _billingBlobService.DeleteBlobFromContainerAsync(BillingConstants.BillingContainerName, fullFilePath);
            }
        }

        private async Task<HandlerResult> Process277(EdiDownloadData ediDownloadata, List<IEdiItem> ediItems, string ediText)
        {
            var transactionResult = await _billingFilePath.GetTransactionControlNumber(ediText);
            var result = await _billingFilePath.FetchClaimSubmissionDataForERA(transactionResult);

            byte[] data = Encoding.UTF8.GetBytes(ediText);
            ediDownloadata.EdiData = ediText;
            var billingBlobModel = new BillingFolderStructureModel
            {
                Data = System.Text.Encoding.UTF8.GetBytes(ediText),
                FileName = ediDownloadata.FileIdentifier,
                AccountInfoId = ediDownloadata.AccountInfoId,
                ClearingHouseId = ediDownloadata.ClearingHouseId
            };

            try
            {
                await _claimAckParser.ParseAsync(ediDownloadata, ediItems, data, result, ediDownloadata.FileIdentifier);
            }
            catch (Exception ex)
            {
                await UploadIntoBillingBlob(billingBlobModel, BlobFolderNames.Errors, null);

                LogError($"Error parsing 277 document - skipping: fileID={ediDownloadata.FileIdentifier} Error={ex.Message}", ex);
                return HandlerResult.Failed();
            }

            await UploadIntoBillingBlob(billingBlobModel, BlobFolderNames.Archive, null);

            return HandlerResult.Success();
        }

        private async Task<HandlerResult> Process999(EdiDownloadData ediDownloadData, List<IEdiItem> ediItems, string ediText)
        {
            var transactionControlResult = await _billingFilePath.GetTransactionControlNumber(ediText);
            var result = await _billingFilePath.FetchClaimSubmissionDataForERA(transactionControlResult);
            byte[] data = Encoding.UTF8.GetBytes(ediText);
            ediDownloadData.EdiData = ediText;
            var billingBlobModel = new BillingFolderStructureModel
            {
                Data = System.Text.Encoding.UTF8.GetBytes(ediText),
                FileName = ediDownloadData.FileIdentifier,
                AccountInfoId = ediDownloadData.AccountInfoId,
                ClearingHouseId = ediDownloadData.ClearingHouseId
            };

            try
            {
                await _edi999Parser.ParseAsync(ediDownloadData, ediItems, result.Id, ediDownloadData.FileIdentifier, data);
            }
            catch (Exception ex)
            {
                LogError($"Error parsing 999 document - skipping: fileID={ediDownloadData.FileIdentifier} Error={ex.Message}", ex);
                await UploadIntoBillingBlob(billingBlobModel, BlobFolderNames.Errors, null);
                return HandlerResult.Failed();
            }

            await UploadIntoBillingBlob(billingBlobModel, BlobFolderNames.Archive, null);

            return HandlerResult.Success();
        }

        private async Task SaveDocuments(List<PaymentEntity> eraDocuments)
        {
            /*
            foreach (var eraDocument in eraDocuments)
            {
                _dbContext.Payment.Add(eraDocument);
            }
            */

            await _dbContext.SaveChangesAsync();

        }

        private async Task SendMessageToTopic(List<PaymentEntity> payment)
        {
            var claimTransactionData = new List<ClaimTransactionModel>();
            foreach (var item in payment)
            {
                var serviceLineIds = item.PaymentClaims.SelectMany(x => x.PaymentClaimServiceLines.Select(x => x.Id).ToList()).ToList();
                foreach (var serviceLine in serviceLineIds)
                {
                    claimTransactionData.Add(PrepareClaimTransaction(serviceLine, ClaimTransactionType.eraReceived));
                }

                var serviceLineIdAdjustments = item.PaymentClaims.SelectMany(x => x.PaymentClaimServiceLines.SelectMany(x => x.PaymentClaimServiceLineAdjustments)).ToList();
                foreach (var serviceLineIdAdjustment in serviceLineIdAdjustments)
                {
                    var transactionType = serviceLineIdAdjustment.AdjustmentGroupCode != "PR" ? ClaimTransactionType.adjustment : ClaimTransactionType.patientResponsibility;
                    claimTransactionData.Add(PrepareClaimTransaction(serviceLineIdAdjustment.Id, transactionType));
                }
            }
            try
            {
                await _messageBus.SendBatchAsync(Topics.RT_Billing_ProcessClaimTxn, claimTransactionData);
            }
            catch (Exception) { }
        }

        private void LogMsg(string msg)
        {
            var msgToWrite = $"[{DateTime.Now:G}]: {msg}";
            _logger.LogInformation(msgToWrite);
        }

        private void LogError(string msg, Exception ex)
        {
            LastError = new EraParserError()
            {
                Message = msg,
                Exception = ex
            };
            var msgToWrite = $"[{DateTime.Now:G}]: {msg}";
            _logger.LogError(msgToWrite);
        }

        private async Task UploadErrorsToBlobStorage(int accountInfoId, string fileIdentifier, List<PaymentEntity> eraPayments)
        {
            ERAUploadModel model = new ERAUploadModel();
            model.PaymentIds = eraPayments.Select(x => x.Id).ToList();
            var (eSuccess, eResult) = await GenerateERAErrors(model);

            if (eSuccess && !string.IsNullOrEmpty(eResult))
            {
                _logger.LogInformation($"Starting uploading error file with payment Ids: {model.PaymentIds}");

                model.accountInfoId = accountInfoId;
                model.data = Encoding.UTF8.GetBytes(eResult);
                model.fileName = $"error_{fileIdentifier}.txt";
                model.containerName = ContainerName;

                var uploadSuccess = await UploadERAErrorsToBlobStorage(model);
                if (uploadSuccess)
                {
                    _logger.LogInformation($"ERA error file uploaded to Azure blob storage for Payment Ids Id : {string.Join(",", model.PaymentIds)}");
                }
                else
                {
                    _logger.LogError($"EDI file upload to Azure blob storage failed for Claim Id : {string.Join(",", model.PaymentIds)}");
                }
            }



        }

        private async Task UploadErrorDueToMissingAccountInfoId(string fileIdentifier, string claimIdentifier, string ediText)
        {

            //var errorContent = $"[ERROR] AccountInfoId not found for ClaimIdentifier '{claimIdentifier}' in DB.\nFile: {fileIdentifier}\nTimestamp: {DateTime.UtcNow:G}";

            var model = new ERAUploadModel

            {

                PaymentIds = new List<int>(), // No payment IDs

                accountInfoId = 0,

                data = Encoding.UTF8.GetBytes(ediText),

                fileName = fileIdentifier,

                containerName = ContainerName,

                claimIdentifier = claimIdentifier

            };

            var uploadSuccess = await UploadERAErrorsToBlobStorage(model);

            if (uploadSuccess)

            {

                _logger.LogInformation($"Uploaded missing AccountInfoId error file: {model.fileName}");

            }

            else

            {

                _logger.LogError($"Failed to upload missing AccountInfoId error file: {model.fileName}");

            }

        }

        #region "Common Service Code"
        public async Task<(bool success, string result)> GenerateERAErrors(ERAUploadModel model)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("XApiKey", _XApiKey);
                client.BaseAddress = new Uri(_ApiUrl + "/ClearingHouse/GenerateERAErrorData");
                var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");
                while (true)
                {
                    var response = await client.PostAsync(client.BaseAddress, content);
                    var responseData = await response.Content.ReadAsStringAsync();
                    var result = Regex.Replace(JsonConvert.DeserializeObject<string>(responseData), "[\"“”]", string.Empty);
                    if (response.IsSuccessStatusCode)
                    {
                        return (true, result);
                    }
                    else if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        return (false, result);
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromSeconds(30));
                    }
                }
            }
        }

        public async Task<bool> UploadERAErrorsToBlobStorage(ERAUploadModel model)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("XApiKey", _XApiKey);
                client.BaseAddress = new Uri(_ApiUrl + "/ClearingHouse/UploadERAErrorFileToBlobStorage");
                var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");
                while (true)
                {
                    var response = await client.PostAsync(client.BaseAddress, content);
                    var responseData = await response.Content.ReadAsStringAsync();
                    var result = Regex.Replace(JsonConvert.DeserializeObject<string>(responseData), "[\"“”]", string.Empty);
                    if (response.IsSuccessStatusCode)
                    {
                        return true;
                    }
                    else if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        return false;
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromSeconds(30));
                    }
                }
            }
        }
        #endregion
    }
}
