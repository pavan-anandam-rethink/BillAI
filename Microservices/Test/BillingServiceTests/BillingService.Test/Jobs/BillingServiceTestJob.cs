using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models;
using Microsoft.Extensions.Logging;
using Quartz;
using Rethink.Services.Common.Enums.BH;
using Rethink.Services.Common.Enums.Billing;
using System;
using System.Threading.Tasks;

namespace BillingService.Test.Jobs
{
    [DisallowConcurrentExecution]
    public class BillingServiceTestJob : IJob
    {
        private readonly ILogger<Worker> _logger;
        private readonly IClaimManagerService _claimManagerService;
        private readonly IClaimService _claimService;
        private bool _testRun = false;

        public BillingServiceTestJob(ILogger<Worker> logger,
                                     IClaimManagerService claimManagerService)
        {
            _logger = logger;
            _claimManagerService = claimManagerService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
#if DEBUG   
#else
            _testRun = true;
#endif 

            if (_testRun)
                return;
            _testRun = true;

            //await TestClaim837Validation(testBadClaimId, testMemberId);
            //await TestClaim837EdiGeneration(testClaimSubmissionId);
            //var hfcaCreateDetails = await TestClaimHcfaClaimSubmissionCreate(testAccountInfoID,  testClaimId);

            //var hfcaGetDetails = await TestClaimHcfaClaimSubmissionGet(testAccountInfoID,  testClaimId);


            //var claimSubmissionId = await TestClaim837ClaimSubmissionCreate(testClaimId);

            //await TestClaim837EdiGeneration(claimSubmissionId);

            // var replacedClaimSubmissionId = await TestClaim837ReplacementClaimSubmission(testClaimId);
            //
            // await TestClaim837EdiGeneration(replacedClaimSubmissionId);
            //
            // var voidedClaimSubmissionId = await TestClaim837VoidClaimSubmission(testClaimId);
            //
            // await TestClaim837EdiGeneration(voidedClaimSubmissionId);


        }

        //private async Task TestClaim837Validation(int claimId, int memberId)
        //{
        //    _logger.LogDebug("Testing 837 Claim Validation");
        //    await _claimManagerService.ValidateClaimData(claimId, memberId);
        //    var result = await _claimService.GetClaimErrorsAndAlertsAsync(claimId);
        //    if (result.Count > 0)
        //    {
        //        Console.WriteLine($"Errors for claim Id= {claimId}: ");
        //        var idx = 1;
        //        foreach (var error in result)
        //        {
        //            Console.WriteLine($"\t{idx} {error.Type}: {error.ErrorCode} [{error.Source}]. {error.Description}");
        //            idx += 1;
        //        }
        //    }
        //    else
        //    {
        //        Console.WriteLine($"No errors for claim Id= {claimId}");
        //    }
        //}
        private async Task<int> TestClaim837ClaimSubmissionCreate(int claimId)
        {

            _logger.LogDebug("Testing 837 Claim Submission (original)");

            var claimSubmissionId = await _claimManagerService.SubmitInitialClaim(claimId, 0, ClaimDocumentType.Doc837P);
            Console.WriteLine($"Created ORIGINAL Claim Submission ID={claimSubmissionId}");
            return claimSubmissionId;
        }
        private async Task<ClaimHFCAModel> TestClaimHcfaClaimSubmissionCreate(int accountInfoId, int claimId)
        {

            _logger.LogDebug("Testing HFCA Claim Submission (original)");

            var result = await _claimManagerService.CreateHCFAClaim(0,
                                                                    accountInfoId, claimId,
                                                                    ClaimFrequencyType.Original,
                                                                    ClaimSubmissionType.Original,
                                                                    ResponsibilitySequenceType.Primary);
            Console.WriteLine($"Created ORIGINAL HFCA Claim Submission ID={result.Id}");
            return result;
        }
        private async Task<ClaimHFCAModel> TestClaimHcfaClaimSubmissionGet(int accountInfoId, int claimId)
        {

            _logger.LogDebug("Testing HFCA Claim Submission (original)");

            var result = await _claimManagerService.LookupHCFAClaimDetails(0, accountInfoId, claimId);

            Console.WriteLine($"Lookup HFCA Claim Submission ID={result.Id}");
            return result;
        }
        private async Task<int> TestClaim837ReplacementClaimSubmission(int claimId)
        {

            _logger.LogDebug("Testing 837 Claim Submission (replacement)");

            var claimSubmissionId = await _claimManagerService.SubmitClaimRebill(claimId, 0, ClaimFrequencyType.Replacement);
            Console.WriteLine($"Created REPLACEMENT Claim Submission ID={claimSubmissionId}");
            return claimSubmissionId;
        }
        private async Task<int> TestClaim837VoidClaimSubmission(int claimId)
        {

            _logger.LogDebug("Testing 837 Claim Submission (void)");

            var claimSubmissionId = await _claimManagerService.SubmitClaimRebill(claimId, 0, ClaimFrequencyType.Void);
            Console.WriteLine($"Created VOID Claim Submission ID={claimSubmissionId}");
            return claimSubmissionId;
        }
        //private async Task TestClaim837EdiGeneration(int testClaimSubmissionId)
        //{
        //    _logger.LogDebug("Testing 837 Claim EDI Generation");
        //    var edi = await _claimManagerService.GenerateEdi(testClaimSubmissionId);
        //    Console.WriteLine(edi);
        //}
    }
}