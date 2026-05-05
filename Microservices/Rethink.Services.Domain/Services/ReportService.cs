using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.History;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.ReportingModels;
using Rethink.Services.Common.Services;
using Rethink.Services.Domain.Interfaces;
using RethinkCore.Messaging.Contracts.Parameters.Mail;
using SummationService.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Rethink.Services.Domain.Services;

public class ReportService(
IRepository<BillingDbContext, PaymentEntity> paymentRepository,
IRepository<BillingDbContext, ClaimHistoryEntity> claimHistoryRepository,
IRethinkMasterDataMicroServices rethinkService,
IHelperService helperService,
IConfiguration configuration,
IRepository<BillingDbContext, ClaimChargeEntryEntity> claimChargeEntryEntity,
IRepository<BillingDbContext, ClaimEntity> claimEntity,
IKeyVaultProviderService keyVaultProviderService) : BaseService, IReportService
{
    //Rethink Mail  

    private readonly string _rethinkMailScopes = configuration["RethinkMailScopes"];
    private readonly string _rethinkMailAPI = configuration["RethinkMailAPI"];

    private readonly IRepository<BillingDbContext, PaymentEntity> _paymentRepository = paymentRepository;
    private readonly IRepository<BillingDbContext, ClaimHistoryEntity> _claimHistoryRepository = claimHistoryRepository;
    private readonly IRepository<BillingDbContext, ClaimChargeEntryEntity> _claimChargeEntryEntity = claimChargeEntryEntity;
    private readonly IRethinkMasterDataMicroServices _rethinkService = rethinkService;
    private readonly IHelperService _helperService = helperService;
    private string reportMonth, reportYear = string.Empty;
    private readonly IKeyVaultProviderService _keyVaultProviderService = keyVaultProviderService;
    private readonly IRepository<BillingDbContext, ClaimEntity> _claimEntity = claimEntity;

    public async Task<bool> SendMonthlyReportAsync(ReportQueryModel dateRange)
    {
        //Set date range - Previous Month's start and end dates
        var today = DateTime.Today;
        var month = new DateTime(today.Year, today.Month, 1);
        var startDate = month.AddMonths(-1);
        var endDate = month;
        dateRange ??= new ReportQueryModel
        {
            StartDate = startDate,
            EndDate = endDate
        };

        reportMonth = startDate.ToString("MMM");
        reportYear = startDate.ToString("yyyy");

        List<ReportResponseModel> submissions, eraPayments, billingAccounts = [];

        //Retrieve the list of all billing-enabled accounts
        //var result = await _rethinkService.GetBillingAccountsAsync();
        var result = (await _rethinkService.GetBillingAccountsAsync()).Where(ac => ac.isTestAccount != true).ToList();
        foreach (AccountModel account in result)
        {
            billingAccounts.Add(new ReportResponseModel() { AccountID = account.id, AccountName = account.name });
        }

        //Retrieve the number of submissions to Availity by account ID
        submissions = [.. _claimHistoryRepository.Query().AsNoTracking()
                            .Include(x => x.Claim)
                            .Where(ch => ch.ClaimId == ch.Claim.Id &&
                                (ch.ClaimHistoryAction == ClaimHistoryAction.MovedToBilledPending ||
                                ch.ClaimHistoryAction == ClaimHistoryAction.BillNextFunder)
                                && ch.ActionDate >= startDate
                                && ch.ActionDate <= endDate)
                            .GroupBy(ch => ch.Claim.AccountInfoId)
                            .Select(g => new ReportResponseModel()
                            {
                                AccountID = g.Key,
                                Count837 = g.Count()
                            })];

        //Retrieve the number of ERA files received (Manual and from Availity) by account ID
        eraPayments = [.. _paymentRepository.Query().AsNoTracking()
                                .Where(p => new[] { 1, 2 }.Contains(p.PaymentTypeId)
                                    && p.ReceivedDate >= startDate
                                    && p.ReceivedDate <= endDate)
                                .GroupBy(p => p.AccountInfoId)
                                .Select(g => new ReportResponseModel()
                                {
                                    AccountID = (int)g.Key,
                                    Count835 = g.Count()
                                })];


        //Combine the above two
        var submissionsAndEraPayments = submissions.Union(eraPayments)
                                .GroupBy(p => new { p.AccountID })
                                .Select(g => new ReportResponseModel()
                                {
                                    AccountID = g.Key.AccountID,
                                    Count835 = g.Sum(p => p.Count835),
                                    Count837 = g.Sum(p => p.Count837),
                                }).ToList();

        //Prepare the report
        var monthlyReport = billingAccounts.Union(submissionsAndEraPayments)
                                .GroupBy(p => new { p.AccountID })
                                .Select(g => new ReportResponseModel()
                                {
                                    AccountID = g.Key.AccountID,
                                    AccountName = billingAccounts.Find(x => x.AccountID == g.Key.AccountID)?.AccountName ?? string.Empty,
                                    Count835 = g.Sum(p => p.Count835),
                                    Count837 = g.Sum(p => p.Count837),
                                    ReportFrequency = ReportFrequency.Monthly
                                }).ToList();

        //For account IDs that generated Availity transactions but unavailable in the Billing-enabled accounts 
        foreach (var account in monthlyReport.Select(x => x))
        {
            var accountDetails = await _rethinkService.GetAccountReturningEntityAsync(account.AccountID, false);
            account.AccountName =  string.IsNullOrEmpty(account.AccountName) ? accountDetails?.Name : account.AccountName;
            account.ReportFrequency = ReportFrequency.Monthly;
            account.IntactId = accountDetails.tProId;
            account.OSB = accountDetails.subscriptionFeatures != null
                                 && accountDetails.subscriptionFeatures.ContainsKey("showOSBFlag")
                                 && (bool)accountDetails.subscriptionFeatures["showOSBFlag"];
        }
        return await MailReport(monthlyReport);

    }
    public async Task<bool> SendWeeklyReportAsync(ReportQueryModel dateRange)
    {
        List<ReportResponseModel> submissions, billingAccounts = [];
        // Retrieve the list of all billing-enabled accounts and exclude test accounts
        var result = (await _rethinkService.GetBillingAccountsAsync()).Where(ac => ac.isTestAccount != true).ToList();
        foreach (AccountModel account in result)
        {
            billingAccounts.Add(new ReportResponseModel() { AccountID = account.id, AccountName = account.name });
        }

        // Join ClaimHistory and ClaimChargeEntry to get submission count and total billed
        submissions = [];

        var claimHistoryQuery = _claimHistoryRepository.Query();
        var claimChargeEntryQuery = _claimChargeEntryEntity.Query();
        var claimsQuery = _claimEntity.Query();

        if (claimHistoryQuery == null)
            throw new InvalidOperationException("_claimHistoryRepository.Query() returned null.");
        if (claimChargeEntryQuery == null)
            throw new InvalidOperationException("_claimChargeEntryEntity.Query() returned null.");

        var sevenDaysAgo = DateTime.Now.AddDays(-7);
        var now = DateTime.Now;

        // Step 1: Distinct Claims
        var distinctClaims =
            claimHistoryQuery
                .AsNoTracking()
                .Where(ch =>
                    (ch.ClaimHistoryAction == ClaimHistoryAction.MovedToBilledPending || ch.ClaimHistoryAction == ClaimHistoryAction.BillNextFunder) &&
                    ch.ActionDate >= sevenDaysAgo &&
                    ch.ActionDate < now)
                .Select(ch => ch.ClaimId)
                .Distinct().ToList();


        // Step 2: Aggregate Charges Per Claim
        var claimCharges =
            claimChargeEntryQuery
                .AsNoTracking()
                .Where(ce => distinctClaims.Contains(ce.ClaimId))
                .GroupBy(ce => ce.ClaimId)
                .Select(g => new
                {
                    ClaimId = g.Key,
                    TotalCharges = g.Sum(x => (decimal?)x.Charges) ?? 0
                }).ToList();


        // Step 3: Final Query
        submissions =
        (
            from dc in distinctClaims
            join c in claimsQuery.AsNoTracking()
                on dc equals c.Id
            join cc in claimCharges
                on dc equals cc.ClaimId into ccGroup
            from cc in ccGroup.DefaultIfEmpty() // LEFT JOIN
            group cc by c.AccountInfoId into g
            select new ReportResponseModel
            {
                AccountID = g.Key,
                Count837 = g.Count(),
                TotalBilled = g.Sum(x => x != null ? x.TotalCharges : 0)
            }
        ).ToList();



        // Prepare the report
        var weeklyReport = billingAccounts.Union(submissions)
            .GroupBy(p => new { p.AccountID })
            .Select(g => new ReportResponseModel()
            {
                AccountID = g.Key.AccountID,
                AccountName = billingAccounts.Find(x => x.AccountID == g.Key.AccountID)?.AccountName ?? string.Empty,
                Count837 = g.Sum(p => p.Count837),
                TotalBilled = g.Sum(p => p.TotalBilled),
                ReportFrequency = ReportFrequency.Weekly
            }).ToList();

        // For account IDs that generated Availity transactions but unavailable in the Billing-enabled accounts 
        foreach (var account in weeklyReport.Where(x => x.AccountName == string.Empty))
        {
            var accountDetails = await _rethinkService.GetAccountReturningEntityAsync(account.AccountID, false);
            account.AccountName = accountDetails?.Name ?? string.Empty;
            account.ReportFrequency = ReportFrequency.Weekly;
        }
        return await MailReport(weeklyReport);
    }
    private byte[] CreateExcelReport(List<ReportResponseModel> monthlyReport)
    {
        var memoryStream = new MemoryStream();
        SpreadsheetDocument document = null;
        try
        {
            document = SpreadsheetDocument.Create(memoryStream, DocumentFormat.OpenXml.SpreadsheetDocumentType.Workbook);
            var workBookPart = document.AddWorkbookPart();
            workBookPart.Workbook = new Workbook();
            var workSheetPart = workBookPart.AddNewPart<WorksheetPart>();

            // Fully qualify the 'Columns' type to resolve ambiguity
            DocumentFormat.OpenXml.Spreadsheet.Columns columns = new();
            columns.Append(new DocumentFormat.OpenXml.Spreadsheet.Column() { Min = 1, Max = 18, Width = 13, CustomWidth = true });

            workSheetPart.Worksheet = new Worksheet(columns, new SheetData());
            var sheets = workBookPart.Workbook.AppendChild(new Sheets());
            if (monthlyReport.FirstOrDefault().ReportFrequency == ReportFrequency.Monthly)
                sheets.AppendChild(new Sheet() { Id = workBookPart.GetIdOfPart(workSheetPart), SheetId = 1, Name = "Monthly Report" });
            if (monthlyReport.FirstOrDefault().ReportFrequency == ReportFrequency.Weekly)
                sheets.AppendChild(new Sheet() { Id = workBookPart.GetIdOfPart(workSheetPart), SheetId = 1, Name = "Weekly Report" });

            var sheetData = workSheetPart.Worksheet.GetFirstChild<SheetData>();

            _helperService.DefineStyles(workBookPart);

            int rowIndex = 0;
            Row headerRow = new() { RowIndex = (UInt32Value)(uint)(rowIndex + 1) };
            if (monthlyReport.FirstOrDefault().ReportFrequency == ReportFrequency.Monthly)
            {
                headerRow.AppendChild(_helperService.AddCell(ExcelCellType.header, "IntactId"));
            }
            headerRow.AppendChild(_helperService.AddCell(ExcelCellType.header, "Account ID"));
            headerRow.AppendChild(_helperService.AddCell(ExcelCellType.header, "Account Name"));
            headerRow.AppendChild(_helperService.AddCell(ExcelCellType.header, "837 Count"));

            if (monthlyReport.FirstOrDefault().ReportFrequency == ReportFrequency.Monthly)
            {
                headerRow.AppendChild(_helperService.AddCell(ExcelCellType.header, "835 Count"));
                headerRow.AppendChild(_helperService.AddCell(ExcelCellType.header, "OSB"));
                headerRow.AppendChild(_helperService.AddCell(ExcelCellType.header, "Total"));

            }
            if (monthlyReport.FirstOrDefault().ReportFrequency == ReportFrequency.Weekly)
                headerRow.AppendChild(_helperService.AddCell(ExcelCellType.header, "Total Billed"));

            sheetData.AppendChild(headerRow);
            bool isColor = false;
            rowIndex = 1;
            foreach (var data in monthlyReport)
            {
                Row row = new() { RowIndex = (UInt32Value)(uint)(rowIndex + 1) };
                if (monthlyReport.FirstOrDefault().ReportFrequency == ReportFrequency.Monthly)
                {
                    row.AppendChild(_helperService.AddCell(ExcelCellType.number, Convert.ToString(data.IntactId), isColor));
                }
                row.AppendChild(_helperService.AddCell(ExcelCellType.number, data.AccountID, isColor));
                row.AppendChild(_helperService.AddCell(ExcelCellType.character, data.AccountName, isColor));
                row.AppendChild(_helperService.AddCell(ExcelCellType.number, data.Count837, isColor));
                if (monthlyReport.FirstOrDefault().ReportFrequency == ReportFrequency.Monthly)
                {
                    row.AppendChild(_helperService.AddCell(ExcelCellType.number, data.Count835, isColor));
                    row.AppendChild(_helperService.AddCell(ExcelCellType.character, data.OSB ? "Yes" : "No", isColor));
                    row.AppendChild(_helperService.AddCell(ExcelCellType.number, data.Total, isColor));
                }
                if (monthlyReport.FirstOrDefault().ReportFrequency == ReportFrequency.Weekly)
                {
                    // Add "$"+data.TotalBilled with right alignment
                    // Replace this block in CreateExcelReport method
                    if (monthlyReport.FirstOrDefault().ReportFrequency == ReportFrequency.Weekly)
                    {
                        // Format TotalBilled as string with $ and two decimals
                        var totalBilledValue = (data.TotalBilled != null && decimal.TryParse(data.TotalBilled.ToString(), out var tb) && tb > 0 ? "$" : "") + Convert.ToDecimal(data.TotalBilled).ToString("N2");
                        // Create cell manually and set right alignment
                        var totalBilledCell = new Cell
                        {
                            DataType = CellValues.String,
                            CellValue = new CellValue(totalBilledValue),
                            StyleIndex = 4U // Use 4U for alternate row color, 3U for default right-aligned style
                        };
                        row.AppendChild(totalBilledCell);
                    }
                }

                sheetData.AppendChild(row);
                rowIndex++;
                isColor = !isColor;
            }
            workBookPart.Workbook.Save();

        }
        catch (Exception ex)
        {
            throw new Exception("An error occured while generating the excel file.", ex);
        }
        finally
        {
            document?.Dispose();
        }
        return memoryStream.ToArray();
    }
    private async Task<bool> MailReport(List<ReportResponseModel> report)
    {
        // Determine environment and production flag
        var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Unknown";
        var isProd = environment.Equals("Production", StringComparison.OrdinalIgnoreCase);

        // Prepare subject, html content, and attachment file name based on report frequency
        string subject, htmlContent, attachmentFileName;
        if (report.FirstOrDefault()?.ReportFrequency == ReportFrequency.Monthly)
        {
            subject = $"Monthly Report for {reportMonth} {reportYear}{(isProd ? string.Empty : $" From {environment}")}";
            htmlContent = "<p>Please find attached the Monthly report</p>";
            attachmentFileName = $"{reportMonth}_{reportYear}_MonthlyReport.xlsx";
        }
        else if (report.FirstOrDefault()?.ReportFrequency == ReportFrequency.Weekly)
        {
            subject = $"Weekly Report from {DateTime.Today.AddDays(-7):dd-MMM-yyyy} to {DateTime.Today:dd-MMM-yyyy}{(isProd ? string.Empty : $" From {environment}")}";
            htmlContent = "<p>Please find attached the weekly report</p>";
            attachmentFileName = "WeeklyReport.xlsx";
        }
        else
        {
            subject = "Rethink Report";
            htmlContent = "<p>Please find attached the report</p>";
            attachmentFileName = "Report.xlsx";
        }

        var toMail = report.FirstOrDefault()?.ReportFrequency == ReportFrequency.Monthly ?
            await _keyVaultProviderService.GetSecretAsync(configuration["RethinkToMailMonthly"]) :
            await _keyVaultProviderService.GetSecretAsync(configuration["RethinkToMailWeekly"]);

        var req = new CreateSendEmailWithAttachmentRequest()
        {
            From = await _keyVaultProviderService.GetSecretAsync(configuration["RethinkFromMail"]),
            FromName = "Rethink Billing Team",
            To = toMail,
            Subject = subject,
            HtmlContent = htmlContent,
            AttachmentFileName = attachmentFileName,
            SenderSource = "Sender Source",
            Attachment = CreateExcelReport(report)
        };

        return await SendEmail(req);
    }

    private async Task<bool> SendEmail(CreateSendEmailWithAttachmentRequest req)
    {
        bool sendEmailSent = false;
        var strEmailIds = req.To.Split(';').ToList();
        foreach (var item in strEmailIds)
        {

            var rethinkMailClientId = _keyVaultProviderService.GetSecretAsync(configuration["RethinkMailClientId"]).Result;
            var rethinkMailTenantId = _keyVaultProviderService.GetSecretAsync(configuration["RethinkMailTenantId"]).Result;
            var rethinkMailSecret = _keyVaultProviderService.GetSecretAsync(configuration["RethinkMailSecret"]).Result;
            var clientSecretCredential = new ClientSecretCredential(rethinkMailTenantId, rethinkMailClientId, rethinkMailSecret);
            req.To = item;

            // Get tokens
            var token = await clientSecretCredential.GetTokenAsync(new TokenRequestContext([_rethinkMailScopes]));
            var client = new HttpClient
            {
                BaseAddress = new Uri(_rethinkMailAPI)
            };

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
            StringContent content = new(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync("api/v1/Mail/sendWithAttachment", content);
            sendEmailSent = response.IsSuccessStatusCode;
        }
        return sendEmailSent;

    }
}
