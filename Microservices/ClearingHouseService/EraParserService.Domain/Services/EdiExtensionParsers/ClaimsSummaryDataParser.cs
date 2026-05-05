using EraParserService.Domain.Models.CF;
using Microsoft.Extensions.Logging;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EraParserService.Domain.Services.EdiExtensionParsers
{
    public class ClaimsSummaryDataParser : BaseService, IClaimsSummaryDataParser
    {
        private const string ValuesSeparator = "|";
        private const int ErrorBloskSize = 3;
        private const int KnownFieldsEndIndex = 31;
        private const int PatientIdFieldIndex = 17;
        private const int ErrorFlagFieldIndex = 30;

        private readonly IRepository<BillingDbContext, ClaimValidationErrorEntity> _claimValidationErrorRepository;
        private readonly IRepository<BillingDbContext, ClaimErrorMessageEntity> _claimErrorMessageRepository;
        private readonly IRepository<BillingDbContext, ClaimSubmissionEntity> _claimSubmissionRepository;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        public ClaimsSummaryDataParser(
            IRepository<BillingDbContext, ClaimValidationErrorEntity> claimValidationErrorRepository,
            IRepository<BillingDbContext, ClaimErrorMessageEntity> claimErrorMessageRepository,
            IRepository<BillingDbContext, ClaimSubmissionEntity> claimSubmissionRepository,
            ILoggerFactory loggerFactory)
        {
            _claimValidationErrorRepository = claimValidationErrorRepository;
            _claimErrorMessageRepository = claimErrorMessageRepository;
            _claimSubmissionRepository = claimSubmissionRepository;
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger(GetType());
        }

        public async Task ParseAsync(int accountInfoId, string fileId, string data)
        {
            _logger.LogInformation($"[{DateTime.Now:G}]: Parsing (CF) Claims Summary Data File: {fileId} for Account {accountInfoId}");

            var tokens = data.Split(ValuesSeparator);
            var claimSummary = MapTokensToModel(tokens);

            if (claimSummary.RejectErrors.Any())
            {
                await SaveErrors(claimSummary.RejectErrors, claimSummary.PatientId);
            }
        }

        private ClaimSummaryDataModel MapTokensToModel(string[] tokens)
        {
            var model = new ClaimSummaryDataModel();
            model.PatientId = tokens[PatientIdFieldIndex];
            model.ErrorFlag = tokens[ErrorFlagFieldIndex];

            // Claims received with error messages will pass a value 'M'
            var hasErrors = model.ErrorFlag == "M";
            if (hasErrors)
            {
                // All values after the 'Error Flag' field are error messages.
                var errorsTokens = tokens.Skip(KnownFieldsEndIndex).ToList();
                model.RejectErrors = MapErrorsTokens(errorsTokens);
            }

            return model;
        }

        private List<RejectErrorModel> MapErrorsTokens(List<string> errorsTokens)
        {
            var result = new List<RejectErrorModel>();
            var groupedErrors = errorsTokens.Select((x, index) => new { x, index }).GroupBy(x => x.index / ErrorBloskSize, y => y.x);

            foreach (var block in groupedErrors)
            {
                var blockValues = block.ToList();
                var error = new RejectErrorModel
                {
                    Code = blockValues[0],
                    Message = blockValues[1],
                    Data = blockValues[2],
                };

                result.Add(error);
            }

            return result;
        }

        private async Task SaveErrors(List<RejectErrorModel> errors, string claimSubmissionIdentifier)
        {
            var claimSubmissionEntity = _claimSubmissionRepository.Query().FirstOrDefault(x => x.ClaimSubmissionIdentifier == claimSubmissionIdentifier);
            if (claimSubmissionEntity != null)
            {
                var claimRejectErrorMessage = _claimErrorMessageRepository.Query().FirstOrDefault(x => x.ErrorNumber == ClaimErrorNumber.EraFunderRejected);
                var validationErrorList = new List<ClaimValidationErrorEntity>();

                foreach (var error in errors)
                {
                    var errorMessage = $"{error.Message}. " +
                        $"{(!string.IsNullOrEmpty(error.Data) ? $"Value: {error.Data}. " : string.Empty)}" +
                        $"Code: {error.Code}.";

                    var validationError = new ClaimValidationErrorEntity
                    {
                        ClaimSubmissionId = claimSubmissionEntity.Id,
                        ClaimErrorMessageId = claimRejectErrorMessage.Id,
                        ClaimErrorSource = ClaimErrorSource.Era,
                        ContextMessage = errorMessage,
                        ValidationDate = EstDateTime,
                    };

                    validationErrorList.Add(validationError);
                }

                await _claimValidationErrorRepository.AddRangeAsync(validationErrorList);
                await _claimValidationErrorRepository.CommitAsync();
            }
            else
            {
                _logger.LogError($"Submission entity with Identifier: {claimSubmissionIdentifier} not found");
            }
        }
    }
}
