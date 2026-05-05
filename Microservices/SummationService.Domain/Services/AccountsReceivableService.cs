using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.PatientInvoice;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Entities.Billing.Reporting;
using Rethink.Services.Common.Entities.Billing.Scheduling;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.ReportingModels;
using Rethink.Services.Common.Services;
using Rethink.Services.Common.Utils;
using SummationService.Domain.Interfaces;

namespace SummationService.Domain.Services;

public class AccountsReceivableService(IRepository<ReportingDbContext, AccountsReceivableEntity> accountsReceivableRepository,
    IRepository<BillingDbContext, ClaimEntity> claimRepository,
    IHelperService helperService,
    IRepository<BillingDbContext, PaymentClaimEntity> paymentClaimRepository,
    IRepository<ReportingDbContext, FundersEntity> funderNameReportingRepository,
    IRepository<BillingDbContext, PaymentClaimServiceLineEntity> paymentClaimServiceLineRepository,
    IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity> paymentClaimServiceLineAdjustmentRepository,
    IRepository<BillingDbContext, ClaimChargeEntryEntity> claimChargeEntryRepository,
    IRepository<BillingDbContext, ClaimAppointmentLinkEntity> claimAppointmentLinkRepository,
    IRepository<BillingDbContext, ClaimVersionEntity> claimVersionRepository,
    IRepository<BillingDbContext, ClaimSearchRenderingProviderEntity>  claimSearchRenderingProvidersRepository,
    IRepository<ReportingDbContext, AccountsReceivableEntity> accountReceivableRepository,
    IRepository<ReportingDbContext, ClientsEntity> clientNameReportingRepository,
    IRepository<BillingDbContext, PatientInvoiceDetailsEntity> patientInvoiceDetailsRepository) : BaseService, IAccountsReceivableService
{

    public async Task<bool> AddOrUpdateAccountsReceivableAsync(ClaimTransactionType transactionType, int transactionTypeId, CancellationToken cancellationToken)
    {
        int claimId = await FindClaimIdByTransactionTypeIdAsync(transactionType, transactionTypeId, cancellationToken) ?? 0;
        if (claimId != 0)
        {
            ClaimEntity? claim = await GetClaimByIdAsync(claimId, cancellationToken).ConfigureAwait(false);
            if (claim != null)
            {
                AccountsReceivableEntity accountsReceivable = await PrepareAccountsReceivableAsync(transactionType, claim, cancellationToken);
                if (accountsReceivable != null)
                {
                    if (accountsReceivable?.Id == 0)
                    {
                        await AddAccountsReceivableAsync(accountsReceivable, cancellationToken);
                    }
                    else
                    {
                        UpdateAccountsReceivable(accountsReceivable, cancellationToken);
                    }
                    await CommitAccountsReceivableAsync();
                    return true;
                }
            }
        }
        return false;
    }

    public async Task<List<FunderDetailsResponseModel>> GetFundersAsync(CancellationToken cancellationToken)
    {
        var result = new List<FunderDetailsResponseModel>();

        var funderDetails = await funderNameReportingRepository.GetAllAsync();
        foreach (var funder in funderDetails)
        {
            result.Add(new FunderDetailsResponseModel
            {
                FunderId = funder.FunderId,
                FunderName = funder.FunderName,
            });
        }
        return result;
    }
       public async Task<AccountsReceivablesResponseModel> GetAccountsReceivablesAsync(AccountsRecievablesRequestModel model, CancellationToken cancellationToken)
    {
            var result = new AccountsReceivablesResponseModel();

            var accountsReceivables = await helperService
                .GetAccountsReceivableEntitiesByFunderIdAsync(
                    model.PayerOrFunder,
                    model.closingDate,
                    model.AccountInfoId,
                    cancellationToken
                );

            accountsReceivables = accountsReceivables.ToList();

            
            var reports = accountsReceivables.Select(item =>
            {
                var days = (EstDateTime - item.BilledDate.Value.Date).TotalDays;

                return new AccountsReceivablesResponse
                {
                    FunderName = item.FunderName,
                    ClientId = item.ClientId,
                    ClientFirstName = item.ClientFirstName,
                    ClientLastName = item.ClientLastName,
                    ClaimFrom = item.ClaimFrom,
                    ClaimThrough = item.ClaimThrough,
                    ClaimStatus = item.ClaimStatus,
                    BilledDate = item.BilledDate,
                    BilledAmount = item.BilledAmount,
                    Adjustments = (-item.WriteOff + item.PatientResponsibility + item.Adjustments),
                    AdjustedClaimAmount = item.AdjustedClaimAmount,
                    PaymentsReceived = item.PaymentReceived,
                    NetReceivable = item.NetReceivable,
                    OneToThirty = days <= 30 && days > 0 ? item.NetReceivable : 0,
                    ThirtyOneToSixty = days <= 60 && days >= 31 ? item.NetReceivable : 0,
                    SixtyOneToNinty = days <= 90 && days >= 61 ? item.NetReceivable : 0,
                    NintyOneToOneHundredTwenty = days <= 120 && days >= 91 ? item.NetReceivable : 0,
                    MoreThanOneHundredTwenty = days > 120 ? item.NetReceivable : 0,
                };
          
            }).ToList();

        result.totalCount = reports.Count;

        var validProperties = typeof(AccountsReceivablesResponse)
            .GetProperties()
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (model.SortingModels != null && model.SortingModels.Any())
        {
            model.SortingModels = model.SortingModels
                .Where(s => !string.IsNullOrWhiteSpace(s.Field) && validProperties.Contains(s.Field))
                .ToList();
        }

        if (model.SortingModels == null || model.SortingModels.Count == 0)
        {
            model.SortingModels = new List<SortingModel>
            {
                new SortingModel { Field = "BilledDate", Dir = "desc" }
            };
        }

        
        var sortedReports = reports
            .AsQueryable()
            .OrderBy(model.SortingModels)
            .ToList();

       
        var pagedReports = sortedReports.Skip(model.Skip);

        if (model.Take > 0)
            pagedReports = pagedReports.Take(model.Take);

        result.AccountsReceivables = pagedReports.ToList();

        return result;
        }
        
        public async Task<AccountsReceivablesChargeLevelResponseModel> GetAccountsReceivablesChargeLevelAsync(AccountsRecievablesRequestModel model, CancellationToken cancellationToken)
    {
        var result = new AccountsReceivablesChargeLevelResponseModel();

        var (reports, total) = await GetAccountsReceivablesChargeLevel(model);

        result.totalCount = total;

        result.AccountsReceivables = reports;

        return result;
    }

    public async Task<byte[]> ExportToExcelChargeLevelAsync(AccountsRecievablesRequestModel model, AccountsReceivablesChargeLevelResponseModel response, CancellationToken cancellationToken)
    {
        // funderNameList
        var funderNameList = await funderNameReportingRepository.Query().AsNoTracking()
                            .Where(x => model.PayerOrFunder.Contains(x.FunderId))
                            .Select(x => x.FunderName)
                            .ToListAsync();

        model.Take = 0;
        var reports = response.AccountsReceivables;

        var memoryStream = new MemoryStream();
        SpreadsheetDocument? document = null;
        try
        {
            document = SpreadsheetDocument.Create(memoryStream, DocumentFormat.OpenXml.SpreadsheetDocumentType.Workbook);
            var workBookPart = document.AddWorkbookPart();
            workBookPart.Workbook = new Workbook();
            var workSheetPart = workBookPart.AddNewPart<WorksheetPart>();

            Columns columns = new();
            columns.Append(new Column() { Min = 1, Max = 18, Width = 13, CustomWidth = true });

            workSheetPart.Worksheet = new Worksheet(columns, new SheetData());
            var sheets = workBookPart.Workbook.AppendChild(new Sheets());
            DateTime date = EstDateTime.Date;
            string formattedDate = date.ToString("yyyy-MM-dd");
            sheets.AppendChild(new Sheet() { Id = workBookPart.GetIdOfPart(workSheetPart), SheetId = 1, Name = "Charge Level Report_" + formattedDate });

            var sheetData = workSheetPart.Worksheet.GetFirstChild<SheetData>();
            helperService.DefineStyles(workBookPart);
            AddChargeLevelCustomRows(model, sheetData, 5, string.Join(", ", funderNameList));

            int rowIndex = 0;
            Row headerRow = new() { RowIndex = (UInt32Value)(uint)(rowIndex + 5) };
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Payer/Funder"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Client Id"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Client First"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Client Last"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Appointment ID"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Billing Provider"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Rendering Provider"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Billing Code"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Date Of Service"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Billed Date"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Age(Days)"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Expected Amount"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Allowed Amount"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Billed Amount"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Adjustments"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Adjusted Charge Amount"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Patient Payments"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Payments Received"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Net Receivables"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "1-30"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "31-60"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "61-90"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "91-120"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, ">120"));
            sheetData.AppendChild(headerRow);

            bool isColor = false;
            rowIndex = 1;
            decimal totalExpectedAmount = 0;
            decimal totalAllowedAmount = 0;
            decimal totalBilledAmount = 0;
            decimal totalAdjustments = 0;
            decimal totalAdjustedChargeAmount = 0;
            decimal totalPatientPayments = 0;
            decimal totalPaymentsReceived = 0;
            decimal totalNetReceivable = 0;
            decimal totalOneToThirty = 0;
            decimal totalThirtyOneToSixty = 0;
            decimal totalSixtyOneToNinty = 0;
            decimal totalNintyOneToOneHundredTwenty = 0;
            decimal totalMoreThanOneHundredTwenty = 0;
            foreach (var data in reports)
            {
                totalExpectedAmount += data.ExpectedAmount ?? 0;
                totalAllowedAmount += data.AllowedAmount ?? 0;
                totalBilledAmount += data.BilledAmount ?? 0;
                totalAdjustments += data.Adjustments;
                totalAdjustedChargeAmount += data.AdjustedChargeAmount ?? 0;
                totalPatientPayments += data.PatientPayments ?? 0;
                totalPaymentsReceived += data.PaymentsReceived;
                totalNetReceivable += data.NetReceivable ?? 0;
                totalOneToThirty += data.OneToThirty;
                totalThirtyOneToSixty += data.ThirtyOneToSixty;
                totalSixtyOneToNinty += data.SixtyOneToNinty;
                totalNintyOneToOneHundredTwenty += data.NintyOneToOneHundredTwenty;
                totalMoreThanOneHundredTwenty += data.MoreThanOneHundredTwenty;
                Row row = new() { RowIndex = (UInt32Value)(uint)(rowIndex + 5) };
                row.AppendChild(helperService.AddCell(ExcelCellType.character, data.FunderName, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.number, data.ClientId != 0 ? data.ClientId.ToString() : "", isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.character, data.ClientFirstName, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.character, data.ClientLastName, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.character, data.AppointmentId, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.character, data.BillingProvider, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.character, data.RenderingProvider, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.character, data.BillingCode, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.date, data.DateOfService, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.date, data.BilledDate, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.number, data.AgeInDays, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.negativeDecimal, data.ExpectedAmount, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.negativeDecimal, data.AllowedAmount, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.negativeDecimal, data.BilledAmount, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.negativeDecimal, data.Adjustments, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.negativeDecimal, data.AdjustedChargeAmount, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.negativeDecimal, data.PatientPayments, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.negativeDecimal, data.PaymentsReceived, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.negativeDecimal, data.NetReceivable, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.negativeDecimal, data.OneToThirty, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.negativeDecimal, data.ThirtyOneToSixty, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.negativeDecimal, data.SixtyOneToNinty, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.negativeDecimal, data.NintyOneToOneHundredTwenty, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.negativeDecimal, data.MoreThanOneHundredTwenty, isColor));

                sheetData.AppendChild(row);
                rowIndex++;

                isColor = !isColor;
            }

            var totalsList = new List<decimal>(new decimal[24]);
            var values = new decimal[] { totalExpectedAmount, totalAllowedAmount, totalBilledAmount, totalAdjustments, totalAdjustedChargeAmount, totalPatientPayments, totalPaymentsReceived, totalNetReceivable, totalOneToThirty, totalThirtyOneToSixty, totalSixtyOneToNinty, totalNintyOneToOneHundredTwenty, totalMoreThanOneHundredTwenty };

            for (int i = 0; i < values.Length; i++)
            {
                totalsList[11 + i] = values[i];
            }
            TotalCustomRows(model, sheetData, reports.Count, string.Join(", ", funderNameList), totalsList);
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

    private async Task<(List<AccountsReceivablesChargeLevelResponse> Data, int Total)> GetAccountsReceivablesChargeLevel(AccountsRecievablesRequestModel model)
    {
        if (model.SortingModels == null || model.SortingModels.Count == 0)
        {
            model.SortingModels =
            [
                new SortingModel
                {
                    Dir = "desc",
                    Field = "dateOfService"
                }
            ];
        }

        var sortingField = model.SortingModels?.FirstOrDefault()?.Field?.ToLower();
        var sortingDir = model.SortingModels?.FirstOrDefault()?.Dir?.ToLower();

                var computedFields = new HashSet<string>
        {
            "onetothirty",
            "thirtyonetosixty",
            "sixtyonetoninty",
            "nintyonetoonehundredtwenty",
            "morethanonehundredtwenty"
        };

                var isComputedField = computedFields.Contains(sortingField);

        // Get the accounts receivable data with related funder and client information
        var accountReceivable = await (
           from ar in accountReceivableRepository.Query().AsNoTracking().Where(x => x.DateDeleted == null)
           join funder in funderNameReportingRepository.Query().AsNoTracking().Where(x => x.DateDeleted == null)
            on ar.FunderId equals funder.FunderId into funderJoin
           from funder in funderJoin.DefaultIfEmpty()
           join client in clientNameReportingRepository.Query().AsNoTracking().Where(x => x.DateDeleted == null)
           on ar.ClientId equals client.ClientId into clientJoin
           from client in clientJoin.DefaultIfEmpty()
           where model.PayerOrFunder.Contains(ar.FunderId)
           where ar.DateCreated.Date <= model.closingDate.Date && ar.BilledDate.Value.Date <= model.closingDate.Date
           select new AccountsReceivableQueryModel
           {
               FunderName = funder.FunderName,
               ClientId = ar.ClientId,
               ClientFirstName = client.ClientFirstName,
               ClientLastName = client.ClientLastName,
               BilledDate = ar.BilledDate,
               BilledAmount = ar.BilledAmount,
               Adjustments = ar.Adjustment,
               WriteOff = ar.WriteOff,
               PatientResponsibility = ar.PatientResponsibility,
               PaymentReceived = ar.PaymentRecieved,
               NetReceivable = ar.NetRecievable,
               DateCreated = ar.DateCreated,
               DateModified = ar.DateModified,
               ClaimId = ar.ClaimId
           }).ToListAsync();

        // Join with ClaimChargeEntry and other related tables to get charge-level details
        var accountsReceivablesChargeLevel = accountReceivable.AsEnumerable()
        .Join(claimChargeEntryRepository.Query().AsNoTracking().Where(x => x.DateDeleted == null),
            ar => ar.ClaimId,
            cce => cce.ClaimId,
            (ar, cce) => new { ar, cce })
        .Where(x => x.cce.DateDeleted == null)

        .GroupJoin(claimRepository.Query().AsNoTracking(),
            arcce => arcce.ar.ClaimId,
            claim => claim.Id,
            (arcce, claims) => new { arcce, claim = claims.FirstOrDefault() })

        .GroupJoin(claimVersionRepository.Query().AsNoTracking(),
            acc => acc.arcce.ar.ClaimId,
            cv => cv.ClaimId,
            (acc, cvs) => new { acc.arcce.ar, acc.arcce.cce, acc.claim, cv = cvs.FirstOrDefault() })

        .GroupJoin(paymentClaimRepository.Query().AsNoTracking().Where(x => x.DateDeleted == null),
            acccv => acccv.ar.ClaimId,
            pc => pc.ClaimId,
            (acccv, pcs) => new { acccv.ar, acccv.cce, acccv.claim, acccv.cv, pc = pcs.FirstOrDefault() })

        .GroupJoin(paymentClaimServiceLineRepository.Query().AsNoTracking().Where(x => x.DateDeleted == null),
             acccvpc => acccvpc.cce.Id,
             pcsl => pcsl.ClaimChargeEntryId,
            (acccvpc, pcsls) => new { acccvpc.ar, acccvpc.cce, acccvpc.claim, acccvpc.cv, acccvpc.pc, pcsl = pcsls })

        .GroupJoin(claimAppointmentLinkRepository.Query().AsNoTracking(),
            accfull => accfull.ar.ClaimId,
            cal => cal.ClaimId,
            (accfull, cals) => new { accfull.ar, accfull.cce, accfull.claim, accfull.cv, accfull.pc, accfull.pcsl, cal = cals })

        .GroupJoin(patientInvoiceDetailsRepository.Query().AsNoTracking(),
            accfinal => accfinal.cce.Id,
            pind => pind.ChargeId,
            (accfinal, pinds) => new { accfinal.ar, accfinal.cce, accfinal.claim, accfinal.cv, accfinal.pc, accfinal.pcsl, accfinal.cal, pind = pinds.FirstOrDefault() })


        .GroupJoin(claimSearchRenderingProvidersRepository.Query().AsNoTracking().Where(r => r.DateDeleted == null),
            accfinal2 => (accfinal2.claim.RenderingStaffMemberId == -2 || accfinal2.claim.RenderingStaffMemberId == null)
                           ? accfinal2.claim.MemberId
                           : accfinal2.claim.RenderingStaffMemberId,
            renderingProvider => renderingProvider.Id,
            (accfinal2, renderingProviders) => new { accfinal2.ar, accfinal2.cce, accfinal2.claim, accfinal2.cv, accfinal2.pc, accfinal2.pcsl, accfinal2.cal, accfinal2.pind, renderingProvider = renderingProviders.FirstOrDefault() })

        .GroupBy(x => x.cce.Id)
        .Select(g =>
        {
            var x = g.First();
            var appointmentIds = g.SelectMany(y => y.cal.Where(x => x.ClaimChargeEntriesId == y.cce.Id)
                                .Select(x => x.AppointmentId)
                                .Distinct()).ToList();

            var chargeAmount = g.Max(y => (y.pcsl?.FirstOrDefault(x => x.ClaimChargeEntryId == y.cce.Id)?.ChargeAmount ?? 0) == 0
                ? y.cce.Charges : y.pcsl?.FirstOrDefault(x => x.ClaimChargeEntryId == y.cce.Id)?.ChargeAmount) ?? 0;

            return new AccountsReceivablesChargeLevel
            {
                Id = x.cce?.Id ?? x.pind?.ChargeId ?? 0,
                FunderName = g.Select(y => y.ar.FunderName).FirstOrDefault(),
                ClientId = g.Select(y => y.ar.ClientId).FirstOrDefault(),
                ClientFirstName = g.Select(y => y.ar.ClientFirstName).FirstOrDefault(),
                ClientLastName = g.Select(y => y.ar.ClientLastName).FirstOrDefault(),
                AppointmentId = string.Join(",", appointmentIds),
                BillingProvider = g.Max(y => y.cv?.BillingProvider),
                RenderingProvider = g.Max(y => y.renderingProvider?.Name),
                BillingCode = g.Max(y => y.cce.BillingCode),
                DateOfService = g.Max(y => y.cce.DateOfService),
                BilledDate = g.Max(y => y.ar.BilledDate),
                AgeInDays = (DateTime.Now - (g.Max(y => y.ar.BilledDate) ?? DateTime.Now)).Days,
                ExpectedAmount = chargeAmount,
                AllowedAmount = g.Max(y => y.pcsl?.FirstOrDefault(x => x.ClaimChargeEntryId == y.cce.Id)?.AllowedAmount),
                BilledAmount = chargeAmount,
                Adjustments = 0,
                WriteOff = g.Max(y => y.ar.WriteOff),
                PatientResponsibility = 0,
                AdjustedChargeAmount = chargeAmount,
                PatientPayments = g.Max(y => y.pind?.PatientPayments),
                PaymentsReceived = g.Max(y => y.pcsl?.FirstOrDefault(x => x.ClaimChargeEntryId == y.cce.Id)?.PaymentAmount) ?? 0,
                NetReceivable = g.Select(y => y.ar.NetReceivable).FirstOrDefault(),
                DateCreated = g.Max(y => y.ar.DateCreated),
                DateModified = g.Max(y => y.ar.DateModified),
                PaymentClaimServiceLineId = x.pcsl?.FirstOrDefault(p => p.ClaimChargeEntryId == x.cce?.Id)?.Id
            };
        });        

       
        var accountsReceivablesChargeLevelData = accountsReceivablesChargeLevel.ToList();

        var total = accountsReceivablesChargeLevelData.Count;

        // Get the adjustments and PR for each PaymentClaimServiceLineId
        var paymentClaimServiceLineIds = accountsReceivablesChargeLevelData.Select(x => x.PaymentClaimServiceLineId).ToList();
        var paymentClaimsAdjustmentList = await paymentClaimServiceLineAdjustmentRepository.Query()
            .Where(p => paymentClaimServiceLineIds.Contains(p.PaymentClaimServiceLineId) && p.DateDeleted == null).ToListAsync();

        accountsReceivablesChargeLevelData.ForEach(x =>
        {
            var adjustments = paymentClaimsAdjustmentList.Where(p => p.PaymentClaimServiceLineId == x.PaymentClaimServiceLineId).ToList();
            if (adjustments.Count > 0)
            {
                var positiveAdjustment = adjustments.Where(x => x.IsAdjustmentPositive == true && x.AdjustmentGroupCode != "PR"
                                         && x.DateDeleted == null).Sum(x => x.AdjustmentAmount) ?? 0;

                var negativeAdjustment = adjustments.Where(x => (x.IsAdjustmentPositive == false || x.IsAdjustmentPositive == null)
                                         && x.AdjustmentGroupCode != "PR" && x.DateDeleted == null).Sum(x => x.AdjustmentAmount) ?? 0;

                var positivePatientResponsibility = adjustments.Where(x => x.IsAdjustmentPositive == true && x.AdjustmentGroupCode == "PR"
                                                    && x.DateDeleted == null).Sum(x => x.AdjustmentAmount) ?? 0;

                var negativePatientResponsibility = adjustments.Where(x => (x.IsAdjustmentPositive == false || x.IsAdjustmentPositive == null)
                                                    && x.AdjustmentGroupCode == "PR" && x.DateDeleted == null).Sum(x => x.AdjustmentAmount) ?? 0;

                x.Adjustments = positiveAdjustment - negativeAdjustment;
                x.PatientResponsibility = positivePatientResponsibility - negativePatientResponsibility;
            }
        });

        var response = new List<AccountsReceivablesChargeLevelResponse>();

        //foreach (var item in accountsReceivablesChargeLevel)
        accountsReceivablesChargeLevelData.ForEach(item =>
        {
            var daysToCalculate = (EstDateTime.Date - item.BilledDate.Value.Date).TotalDays;
            var days = Math.Round(daysToCalculate);

            var netReceivable = (item.BilledAmount - (item.PatientPayments ?? 0) - (item.PaymentsReceived) + ((-item.WriteOff ?? 0) + (item.PatientResponsibility ?? 0) + (item.Adjustments ?? 0))) ?? 0;
            response.Add(new AccountsReceivablesChargeLevelResponse
            {
                Id = item.Id,
                FunderName = item.FunderName,
                ClientId = item.ClientId,
                ClientFirstName = item.ClientFirstName,
                ClientLastName = item.ClientLastName,
                AppointmentId = string.Join(", ", item.AppointmentId.Split(',', StringSplitOptions.RemoveEmptyEntries).Distinct()),
                BillingProvider = item.BillingProvider,
                RenderingProvider = item.RenderingProvider,
                BillingCode = item.BillingCode,
                DateOfService = item.DateOfService,
                BilledDate = item.BilledDate,
                AgeInDays = (int)item.AgeInDays,
                ExpectedAmount = item.ExpectedAmount ?? 0,
                AllowedAmount = item.AllowedAmount ?? 0,
                BilledAmount = item.BilledAmount ?? 0,
                Adjustments = ((-item.WriteOff ?? 0) + (item.PatientResponsibility ?? 0) + (item.Adjustments ?? 0)),
                AdjustedChargeAmount = (item.BilledAmount + (-item.WriteOff ?? 0) + (item.PatientResponsibility ?? 0) + (item.Adjustments ?? 0)) ?? 0,
                PatientPayments = item.PatientPayments ?? 0,
                PaymentsReceived = item.PaymentsReceived,
                NetReceivable = netReceivable,
                OneToThirty = days <= 30 && days >= 0 ? netReceivable : 0,
                ThirtyOneToSixty = days <= 60 && days >= 31 ? netReceivable : 0,
                SixtyOneToNinty = days <= 90 && days >= 61 ? netReceivable : 0,
                NintyOneToOneHundredTwenty = days <= 120 && days >= 91 ? netReceivable : 0,
                MoreThanOneHundredTwenty = days > 120 ? netReceivable : 0,
            });
        });

        if (isComputedField)
        {
            Func<AccountsReceivablesChargeLevelResponse, object> keySelector = sortingField switch
            {
                "onetothirty" => x => x.OneToThirty,
                "thirtyonetosixty" => x => x.ThirtyOneToSixty,
                "sixtyonetoninty" => x => x.SixtyOneToNinty,
                "nintyonetoonehundredtwenty" => x => x.NintyOneToOneHundredTwenty,
                "morethanonehundredtwenty" => x => x.MoreThanOneHundredTwenty,

                _ => x => x.DateOfService
            };

            response = sortingDir == "asc"
                ? response.OrderBy(keySelector).ToList()
                : response.OrderByDescending(keySelector).ToList();
        }
        else {

            var property = typeof(AccountsReceivablesChargeLevelResponse)
        .GetProperties()
        .FirstOrDefault(p => p.Name.Equals(sortingField, StringComparison.OrdinalIgnoreCase));

            if (property != null)
            {
                response = sortingDir == "asc"
                    ? response.OrderBy(x => property.GetValue(x, null)).ToList()
                    : response.OrderByDescending(x => property.GetValue(x, null)).ToList();
            }
            else
            {
              
                response = response.OrderByDescending(x => x.DateOfService).ToList();
            }

        }

        
        var pagedResponse = response
            .Skip(model.Skip);

        if (model.Take > 0)
            pagedResponse = pagedResponse.Take(model.Take);

        return (pagedResponse.ToList(), total);
       
    }

    public async Task<byte[]> ExportToExcelAsync(AccountsRecievablesRequestModel model, AccountsReceivablesResponseModel response, CancellationToken cancellationToken)
    {
        var result = new AccountsReceivablesResponseModel();
       
        var funderNameList = await funderNameReportingRepository.Query().AsNoTracking()
                            .Where(x => model.PayerOrFunder.Contains(x.FunderId))
                            .Select(x => x.FunderName)
                            .ToListAsync();

        var reports = response.AccountsReceivables;

        var memoryStream = new MemoryStream();
        SpreadsheetDocument document = null;
        try
        {


            document = SpreadsheetDocument.Create(memoryStream, DocumentFormat.OpenXml.SpreadsheetDocumentType.Workbook);
            var workBookPart = document.AddWorkbookPart();
            workBookPart.Workbook = new Workbook();
            var workSheetPart = workBookPart.AddNewPart<WorksheetPart>();

            Columns columns = new Columns();
            columns.Append(new Column() { Min = 1, Max = 18, Width = 13, CustomWidth = true });

            workSheetPart.Worksheet = new Worksheet(columns, new SheetData());
            var sheets = workBookPart.Workbook.AppendChild(new Sheets());
            sheets.AppendChild(new Sheet() { Id = workBookPart.GetIdOfPart(workSheetPart), SheetId = 1, Name = "Account Receivables Reports" });

            var sheetData = workSheetPart.Worksheet.GetFirstChild<SheetData>();

            helperService.DefineStyles(workBookPart);


            AddCustomRows(model, sheetData, 5, string.Join(", ", funderNameList));

            int rowIndex = 0;
            Row headerRow = new Row() { RowIndex = (UInt32Value)(uint)(rowIndex + 5) };
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Payer/Funder"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Client Id"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Client First Name"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Client Last Name"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Claim From"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Claim Through"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Claim Status"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Billed Date"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Billed Amount"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Adjustments"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Adjusted Claim Amount"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Payments Received"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Net Receivables"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "1-30"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "31-60"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "61-90"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "91-120"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, ">120"));
            sheetData.AppendChild(headerRow);

            bool isColor = false;
            rowIndex = 1;
            foreach (var data in reports)
            {
                Row row = new Row() { RowIndex = (UInt32Value)(uint)(rowIndex + 5) };
                row.AppendChild(helperService.AddCell(ExcelCellType.character, data.FunderName, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.number, data.ClientId != 0 ? data.ClientId.ToString() : "", isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.character, data.ClientFirstName, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.character, data.ClientLastName, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.date, data.ClaimFrom, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.date, data.ClaimThrough, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.character, data.ClaimStatus, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.date, data.BilledDate, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.decimalValue, data.BilledAmount, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.negativeDecimal, data.Adjustments, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.negativeDecimal, data.AdjustedClaimAmount, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.negativeDecimal, data.PaymentsReceived, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.negativeDecimal, data.NetReceivable, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.negativeDecimal, data.OneToThirty, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.negativeDecimal, data.ThirtyOneToSixty, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.negativeDecimal, data.SixtyOneToNinty, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.negativeDecimal, data.NintyOneToOneHundredTwenty, isColor));
                row.AppendChild(helperService.AddCell(ExcelCellType.negativeDecimal, data.MoreThanOneHundredTwenty, isColor));

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
    private void AddCustomRows(AccountsRecievablesRequestModel model, SheetData sheetData, int numberOfRows, string funderName)
    {
        for (int i = 0; i < numberOfRows; i++)
        {
            var row = new Row();

            if (i == 0)
            {
                row.AppendChild(new Cell() { DataType = CellValues.String, CellValue = new CellValue("Payer/Funder: "), StyleIndex = (int)ExcelCellDesignStyles.fontWithBoldStyle });
                row.AppendChild(new Cell() { DataType = CellValues.String, CellValue = new CellValue(System.String.Join(", ", funderName)) });
            }
            if (i == 1)
            {
                row.AppendChild(new Cell() { DataType = CellValues.String, CellValue = new CellValue("Closing Date: "), StyleIndex = (int)ExcelCellDesignStyles.fontWithBoldStyle });
                row.AppendChild(new Cell() { DataType = CellValues.String, CellValue = new CellValue(model.closingDate.ToString("MM/dd/yyyy")) });
            }
            if (i == 2)
            {
                var asciiValue = 65;
                var iteration = 7;
                for (var k = 0; k < 13; k++)
                {
                    char character = (char)asciiValue;
                    if (k > iteration)
                    {
                        row.AppendChild(new Cell() { DataType = CellValues.String, CellValue = new CellValue($"({character})") });
                        asciiValue++;
                    }
                    else
                    {
                        row.AppendChild(new Cell() { DataType = CellValues.String, CellValue = new CellValue("") });
                    }
                }
            }
            if (i == 3)
            {
                var FirstFormulaIndexNumber = 10;
                var secondFormulaIndexNumber = 12;
                for (var k = 0; k < 13; k++)
                {
                    if (k == FirstFormulaIndexNumber)
                    {
                        row.AppendChild(new Cell() { DataType = CellValues.String, CellValue = new CellValue("(A+B)") });
                    }
                    else if (k == secondFormulaIndexNumber)
                    {
                        row.AppendChild(new Cell() { DataType = CellValues.String, CellValue = new CellValue("(A+B-D)") });
                    }
                    else
                    {
                        row.AppendChild(new Cell() { DataType = CellValues.String, CellValue = new CellValue("") });
                    }
                }
            }
            sheetData.AppendChild(row);
        }
    }


    private void AddChargeLevelCustomRows(AccountsRecievablesRequestModel model, SheetData sheetData, int numberOfRows, string funderName)
    {
        for (int i = 0; i < numberOfRows; i++)
        {
            var row = new Row();

            if (i == 0)
            {
                row.AppendChild(new Cell() { DataType = CellValues.String, CellValue = new CellValue("Payer/Funder: "), StyleIndex = (int)ExcelCellDesignStyles.fontWithBoldStyle });
                row.AppendChild(new Cell() { DataType = CellValues.String, CellValue = new CellValue(System.String.Join(", ", funderName)) });
            }
            if (i == 1)
            {
                row.AppendChild(new Cell() { DataType = CellValues.String, CellValue = new CellValue("Closing Date: "), StyleIndex = (int)ExcelCellDesignStyles.fontWithBoldStyle });
                row.AppendChild(new Cell() { DataType = CellValues.String, CellValue = new CellValue(model.closingDate.ToString("MM/dd/yyyy")) });
            }
            if (i == 2)
            {
                var asciiValue = 65;
                var iteration = 12;
                for (var k = 0; k < 18; k++)
                {
                    char character = (char)asciiValue;
                    if (k > iteration)
                    {
                        row.AppendChild(new Cell() { DataType = CellValues.String, CellValue = new CellValue($"({character})") });
                        asciiValue++;
                    }
                    else
                    {
                        row.AppendChild(new Cell() { DataType = CellValues.String, CellValue = new CellValue("") });
                    }
                }
            }
            if (i == 3)
            {
                var FirstFormulaIndexNumber = 15;
                var secondFormulaIndexNumber = 18;
                for (var k = 0; k < 19; k++)
                {
                    if (k == FirstFormulaIndexNumber)
                    {
                        row.AppendChild(new Cell() { DataType = CellValues.String, CellValue = new CellValue("(A+B)") });
                    }
                    else if (k == secondFormulaIndexNumber)
                    {
                        row.AppendChild(new Cell() { DataType = CellValues.String, CellValue = new CellValue("(A+B-D-E)") });
                    }
                    else
                    {
                        row.AppendChild(new Cell() { DataType = CellValues.String, CellValue = new CellValue("") });
                    }
                }
            }
            sheetData.AppendChild(row);
        }
    }

    private void TotalCustomRows(AccountsRecievablesRequestModel model, SheetData sheetData, int numberOfRows, string funderName, List<decimal> totalsList)
    {
        for (int i = numberOfRows; i < numberOfRows + 1; i++)
        {
            var row = new Row();

            var FirstIndexNumber = 11;
            for (var k = 0; k < 24; k++)
            {
                if (k == 0)
                {
                    row.AppendChild(new Cell()
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue("Total")
                    });
                }
                else if (k >= FirstIndexNumber && k < totalsList.Count)
                {
                    row.AppendChild(new Cell()
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue(totalsList[k].ToString("N2"))
                    });
                }
                else
                {
                    row.AppendChild(new Cell()
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue(string.Empty)
                    });
                }
            }

            sheetData.AppendChild(row);
        }
    }

    private async Task CommitAccountsReceivableAsync()
    {
        await accountsReceivableRepository.CommitAsync();
    }

    public async Task<int?> FindClaimIdByTransactionTypeIdAsync(ClaimTransactionType transactionType, int transactionTypeId, CancellationToken cancellationToken)
    {
        int? claimId = 0;
        switch (transactionType)
        {
            case ClaimTransactionType.billedAmount:
            case ClaimTransactionType.deleteCharge:
                claimId = await helperService.GetClaimIdFromChargeEntryIdAsync(transactionTypeId, cancellationToken);
                break;
            case ClaimTransactionType.writeOff:
                claimId = await helperService.GetClaimIdFromWriteOffIdAsync(transactionTypeId, cancellationToken);
                break;
            case ClaimTransactionType.deleteChargePayment:
            case ClaimTransactionType.insurancePayment:
            case ClaimTransactionType.eraReceived:
                claimId = await helperService.GetClaimIdFromPaymentIdAsync(transactionTypeId, cancellationToken);
                break;
            case ClaimTransactionType.adjustment:
            case ClaimTransactionType.patientResponsibility:
                claimId = await helperService.GetClaimIdFromAdjustmentIdAsync(transactionTypeId, cancellationToken);
                break;
            case ClaimTransactionType.deleteClaim:
            case ClaimTransactionType.submitClaim:
                claimId = transactionTypeId;
                break;
            //Not applicable for AR reporting
            case ClaimTransactionType.patientPayment:
            case ClaimTransactionType.otherPayment:
            case ClaimTransactionType.newDay://Internal use for AR
            case ClaimTransactionType.updatePaymentSummary:
            default:
                break;
        }
        return claimId;
    }

    public async Task<ClaimEntity?> GetClaimByIdAsync(int claimId, CancellationToken cancellationToken)
    {
        return await claimRepository.Query().Where(x => x.Id == claimId).FirstOrDefaultAsync(cancellationToken);
    }

    private async Task SetClaimDatesAsync(AccountsReceivableEntity accountsReceivable, CancellationToken cancellationToken)
    {
        var claimChargeEntries = await helperService.GetChargeEntriesByClaimId(accountsReceivable.ClaimId);
        accountsReceivable.ClaimFrom = claimChargeEntries.Any() ? claimChargeEntries.OrderBy(x => x.Id).Select(x => x.DateOfService).FirstOrDefault() : accountsReceivable.ClaimFrom;
        accountsReceivable.ClaimThrough = claimChargeEntries.Any() ? claimChargeEntries.OrderByDescending(x => x.Id).Select(x => x.DateOfService).FirstOrDefault() : accountsReceivable.ClaimThrough;
    }

    public async Task<AccountsReceivableEntity?> GetAccountsReceivableByIdAsync(int claimId, CancellationToken cancellationToken)
    {
        return await accountsReceivableRepository.Query().Where(x => x.ClaimId == claimId && x.DateDeleted == null).OrderByDescending(x => x.Id).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<AccountsReceivableEntity?> PrepareAccountsReceivableAsync(ClaimTransactionType transactionType, ClaimEntity claim, CancellationToken cancellationToken)
    {
        //Check if Billed
        if (!claim.billedDate.HasValue)
        {
            return null;
        }
        var accountsReceivable = await GetAccountsReceivableByIdAsync(claim.Id, cancellationToken);
        //New record if not created or if created date is different from today
        if (accountsReceivable == null || (accountsReceivable.DateCreated.Date != EstDateTime.Date))
        {
            //If claim deleted on a different day than last updated, do not create a new AR entry
            if (transactionType == ClaimTransactionType.deleteClaim)
            {
                return null;
            }
            accountsReceivable = new AccountsReceivableEntity
            {
                ClaimId = claim.Id,
                AccountInfoId = claim.AccountInfoId,
                FunderId = claim.PrimaryFunderId,
                ClientId = claim.ChildProfileId,
                DateCreated = EstDateTime
            };
            transactionType = ClaimTransactionType.newDay;
        }

        accountsReceivable.ClaimStatusId = (int)claim.ClaimStatus;
        accountsReceivable.BilledDate = claim.billedDate;
        await SetClaimDatesAsync(accountsReceivable, cancellationToken);
        await SetTransactionTypeValue(accountsReceivable, transactionType);
        accountsReceivable.AdjustedClaimAmount = accountsReceivable.BilledAmount + accountsReceivable.Adjustment + accountsReceivable.PatientResponsibility - accountsReceivable.WriteOff;
        accountsReceivable.NetRecievable = accountsReceivable.AdjustedClaimAmount - accountsReceivable.PaymentRecieved;
        accountsReceivable.DateModified = EstDateTime;
        return accountsReceivable;
    }

    public async Task AddAccountsReceivableAsync(AccountsReceivableEntity accountsReceivableEntity, CancellationToken cancellationToken)
    {
        await accountsReceivableRepository.AddAsync(accountsReceivableEntity);
    }

    public void UpdateAccountsReceivable(AccountsReceivableEntity accountsReceivableEntity, CancellationToken cancellationToken)
    {
        accountsReceivableRepository.Update(accountsReceivableEntity);
    }

    private async Task<AccountsReceivableEntity> SetTransactionTypeValue(AccountsReceivableEntity accountsReceivable, ClaimTransactionType transactionType)
    {
        switch (transactionType)
        {
            case ClaimTransactionType.billedAmount:
                accountsReceivable.BilledAmount = await CalculateClaimTransationSumAsync(accountsReceivable.ClaimId, transactionType);
                break;
            case ClaimTransactionType.insurancePayment:
            case ClaimTransactionType.eraReceived:
                accountsReceivable.PaymentRecieved = await CalculateClaimTransationSumAsync(accountsReceivable.ClaimId, transactionType);
                break;
            case ClaimTransactionType.patientResponsibility:
            case ClaimTransactionType.adjustment:
                accountsReceivable.Adjustment = await CalculateClaimTransationSumAsync(accountsReceivable.ClaimId, ClaimTransactionType.adjustment);
                accountsReceivable.PatientResponsibility = await CalculateClaimTransationSumAsync(accountsReceivable.ClaimId, ClaimTransactionType.patientResponsibility);
                break;
            case ClaimTransactionType.writeOff:
                accountsReceivable.WriteOff = await CalculateClaimTransationSumAsync(accountsReceivable.ClaimId, transactionType);
                break;
            case ClaimTransactionType.deleteClaim:
                accountsReceivable.DateDeleted = EstDateTime;
                break;
            case ClaimTransactionType.deleteCharge:
            case ClaimTransactionType.submitClaim:
            case ClaimTransactionType.newDay:
                accountsReceivable.BilledAmount = await CalculateClaimTransationSumAsync(accountsReceivable.ClaimId, ClaimTransactionType.billedAmount);
                accountsReceivable.PaymentRecieved = await CalculateClaimTransationSumAsync(accountsReceivable.ClaimId, ClaimTransactionType.insurancePayment);
                accountsReceivable.Adjustment = await CalculateClaimTransationSumAsync(accountsReceivable.ClaimId, ClaimTransactionType.adjustment);
                accountsReceivable.PatientResponsibility = await CalculateClaimTransationSumAsync(accountsReceivable.ClaimId, ClaimTransactionType.patientResponsibility);
                accountsReceivable.WriteOff = await CalculateClaimTransationSumAsync(accountsReceivable.ClaimId, ClaimTransactionType.writeOff);
                break;
            case ClaimTransactionType.deleteChargePayment:
                accountsReceivable.PaymentRecieved = await CalculateClaimTransationSumAsync(accountsReceivable.ClaimId, ClaimTransactionType.insurancePayment);
                accountsReceivable.Adjustment = await CalculateClaimTransationSumAsync(accountsReceivable.ClaimId, ClaimTransactionType.adjustment);
                accountsReceivable.PatientResponsibility = await CalculateClaimTransationSumAsync(accountsReceivable.ClaimId, ClaimTransactionType.patientResponsibility);
                break;
            //Patient Payment, Other Payment not applicable for AR reporting
            case ClaimTransactionType.patientPayment:
            case ClaimTransactionType.otherPayment:
            case ClaimTransactionType.updatePaymentSummary:
            default:
                break;
        }
        return accountsReceivable;
    }

    private async Task<decimal> CalculateClaimAdjustmentSumAsync(int claimId, ClaimTransactionType adjustmentTransactionType)
    {
        var adjustmentAmountList = await helperService.GetAdjustmentsFromClaimIdAsync(claimId, adjustmentTransactionType);

        return CalculateOverallAdjustment(adjustmentAmountList);
    }

    private async Task<decimal> CalculateClaimTransationSumAsync(int claimId, ClaimTransactionType transactionType)
    {
        decimal totalAmount = 0;
        switch (transactionType)
        {
            case ClaimTransactionType.billedAmount:
                totalAmount = await helperService.GetBilledAmountByClaimIdAsync(claimId);
                break;
            case ClaimTransactionType.writeOff:
                totalAmount = await helperService.CalculateClaimWriteOffSumAsync(claimId);
                break;
            case ClaimTransactionType.insurancePayment:
            case ClaimTransactionType.patientPayment:
            case ClaimTransactionType.otherPayment:
            case ClaimTransactionType.eraReceived:
                totalAmount = await helperService.CalculateClaimPaymentSumAsync(claimId, FindPaymentTypeId(transactionType));
                break;
            case ClaimTransactionType.adjustment:
            case ClaimTransactionType.patientResponsibility:
                totalAmount = await CalculateClaimAdjustmentSumAsync(claimId, transactionType);
                break;
            default:
                break;
        }
        return totalAmount;
    }
}
