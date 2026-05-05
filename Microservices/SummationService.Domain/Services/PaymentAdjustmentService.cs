using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.WriteOff;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Entities.Billing.Reporting;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.ReportingModels;
using Rethink.Services.Common.Services;
using Rethink.Services.Common.Utils;
using SummationService.Domain.Interfaces;

namespace SummationService.Domain.Services;

public class PaymentAdjustmentService(IRepository<ReportingDbContext, PaymentsAdjustmentsEntity> paymentsAdjustmentsRepository,
    IRepository<BillingDbContext, ClaimEntity> claimRepository,
    IRepository<BillingDbContext, ClaimChargeEntryWriteOffEntity> claimChargeEntryWriteOffRepository,
    IRepository<BillingDbContext, ClaimChargeEntryEntity> claimChargeEntryRepository,
    IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity> paymentClaimServiceLineAdjustmentRepository,
    IRepository<BillingDbContext, PaymentClaimServiceLineEntity> paymentClaimServiceLineRepository,
    IRepository<BillingDbContext, PaymentEntity> paymentRepository,
    IRepository<ReportingDbContext, FundersEntity> funderNameReportingRepository,
    IHelperService helperService) : BaseService, IPaymentAdjustmentService
{
    public async Task<bool> AddOrUpdatePaymentAdjustmentAsync(ClaimTransactionType transactionType, int transactionTypeId, CancellationToken cancellationToken)
    {
        int processingId = await FindClaimIdByTransactionTypeIdAsync(transactionType, transactionTypeId, cancellationToken) ?? 0;

        bool result = false;
        switch (transactionType)
        {
            case ClaimTransactionType.updatePaymentSummary:
                result = await updatePaymentSummaryProcessAsync(processingId, transactionType, cancellationToken);
                break;
            case ClaimTransactionType.deleteCharge:
            case ClaimTransactionType.deleteClaim:
            case ClaimTransactionType.deleteChargePayment:
                result = await PaymentsAdjustmentsDeleteProcessAsync(transactionType, transactionTypeId, processingId, cancellationToken);
                break;
            default:
                result = await ProcessPaymentsAdjustmentsTasksAsync(processingId, transactionType, transactionTypeId, cancellationToken);
                break;
        }
        if (result)
        {
            await CommitPaymentsAdjustmentsAsync();
        }

        return result;
    }


    public async Task<bool> updatePaymentSummaryProcessAsync(int processingId, ClaimTransactionType transactionType, CancellationToken cancellationToken)
    {
        if (processingId != 0)
        {
            PaymentEntity payment = await paymentRepository.Query().Where(x => x.Id == processingId && x.DateDeleted == null).FirstOrDefaultAsync(cancellationToken);
            if (payment != null)
            {
                var paymentsAdjustments = await paymentsAdjustmentsRepository.Query().Where(x => x.PaymentId == payment.Id && x.DateDeleted == null).ToListAsync();

                foreach (var paymentsAdjustment in paymentsAdjustments)
                {
                    await PreparePaymentsAdjustmentsListAsync(transactionType, paymentsAdjustment, payment);
                }
                await UpdatePaymentsAdjustmentListsAsync(paymentsAdjustments, cancellationToken);
                return true;
            }
        }
        return false;
    }

    public async Task<bool> ProcessPaymentsAdjustmentsTasksAsync(int processingId, ClaimTransactionType transactionType, int transactionTypeId, CancellationToken cancellationToken)
    {
        if (processingId != 0)
        {
            ClaimEntity? claim = await GetClaimByIdAsync(processingId, cancellationToken).ConfigureAwait(false);
            if (claim != null)
            {
                PaymentsAdjustmentsEntity? paymentsAdjustments = await PreparePaymentsAdjustmentsAsync(transactionType, claim, transactionTypeId, cancellationToken);
                if (paymentsAdjustments.Id == 0)
                {
                    await AddPaymentsAdjustmentsAsync(paymentsAdjustments, cancellationToken);
                }
                else
                {
                    await UpdatePaymentsAdjustmentsAsync(paymentsAdjustments, cancellationToken);
                }
                return true;
            }
        }
        return false;
    }

    public async Task<bool> PaymentsAdjustmentsDeleteProcessAsync(ClaimTransactionType transactionType, int transactionTypeId, int processingId, CancellationToken cancellationToken)
    {
        List<PaymentsAdjustmentsEntity?> paymentsAdjustmentsList = await PreparePaymentsAdjustmentsForDeleteAsync(transactionType, transactionTypeId, processingId, cancellationToken);
        if (paymentsAdjustmentsList.Count != 0 && paymentsAdjustmentsList != null)
        {
            await UpdatePaymentsAdjustmentListsAsync(paymentsAdjustmentsList, cancellationToken);
        }
        return true;
    }
    public async Task<PaymentsAdjustmentsResponseModel> GetPaymentsAdjustmentsAsync(PaymentsAdjustmentsRequestModel model,CancellationToken cancellationToken)
    {
        if (model.SortingModels == null || model.SortingModels.Count == 0)
        {
            model.SortingModels = new List<SortingModel>
        {
            new SortingModel { Field = "DateModified", Dir = "desc" }
        };
        }
        var sortingField = model.SortingModels?.FirstOrDefault()?.Field?.ToLower();
        var sortingDir = model.SortingModels?.FirstOrDefault()?.Dir?.ToLower();

        var paymentsAdjustments = await helperService
            .GetPaymentsAdjustmentsByFunderIdAndDateAsync(
                model.FunderId,
                model.StartDate,
                model.EndDate,
                (ReportingDateRangeType)model.RangeType,
                model.AccountInfoId,
                cancellationToken
            );

        var reports = paymentsAdjustments.ToList();


        var property = typeof(PaymentsAdjustmentsResponse)
      .GetProperties()
      .FirstOrDefault(p => p.Name.Equals(sortingField, StringComparison.OrdinalIgnoreCase));

        var sortedReports = sortingDir == "asc"
                   ? reports.OrderBy(x => property.GetValue(x, null)).ToList()
                   : reports.OrderByDescending(x => property.GetValue(x, null)).ToList();
        var result = new PaymentsAdjustmentsResponseModel
        {
            totalCount = sortedReports.Count
        };

        if (model.IsExport != true)
        {
            var pagedReports = sortedReports.Skip(model.Skip);
            if (model.Take > 0)
                pagedReports = pagedReports.Take(model.Take);

            result.paymentsAdjustments = pagedReports.ToList();
        }
        else
        {
            result.paymentsAdjustments = sortedReports;
        }

        return result;
    }
    public async Task<byte[]> ExportToExcelAsync(PaymentsAdjustmentsRequestModel model, PaymentsAdjustmentsResponseModel response, CancellationToken cancellationToken)
    {

        if (model.SortingModels == null || model.SortingModels.Count == 0)
        {
            model.SortingModels = new List<SortingModel>
                {
                    new SortingModel
                    {
                        Dir = "desc",
                        Field = "dateModified"
                    }
                };
        }
        var paymentsAdjustments = response.paymentsAdjustments;

        // funderNameList — when Select All (null), get all funder names; otherwise filter by selected IDs
        List<string> funderNameList;
        if (model.FunderId == null || model.FunderId.Count == 0)
        {
            funderNameList = await funderNameReportingRepository.Query().AsNoTracking()
                            .Select(x => x.FunderName)
                            .Distinct()
                            .ToListAsync();
        }
        else
        {
            funderNameList = await funderNameReportingRepository.Query().AsNoTracking()
                            .Where(x => model.FunderId.Contains(x.FunderId))
                            .Select(x => x.FunderName)
                            .ToListAsync();
        }

        paymentsAdjustments = paymentsAdjustments.AsQueryable().OrderBy(model.SortingModels)
                .ToList();


        var memoryStream = new MemoryStream();
        SpreadsheetDocument document = null;
        try
        {


            document = SpreadsheetDocument.Create(memoryStream, DocumentFormat.OpenXml.SpreadsheetDocumentType.Workbook);
            var workBookPart = document.AddWorkbookPart();
            workBookPart.Workbook = new Workbook();
            var workSheetPart = workBookPart.AddNewPart<WorksheetPart>();

            Columns columns = new Columns();
            columns.Append(new Column() { Min = 1, Max = 16, Width = 13, CustomWidth = true });

            workSheetPart.Worksheet = new Worksheet(columns, new SheetData());
            var sheets = workBookPart.Workbook.AppendChild(new Sheets());
            sheets.AppendChild(new Sheet() { Id = workBookPart.GetIdOfPart(workSheetPart), SheetId = 1, Name = "Payments Adjustments Reports" });

            var sheetData = workSheetPart.Worksheet.GetFirstChild<SheetData>();

            helperService.DefineStyles(workBookPart);

            AddCustomRows(model, sheetData, 4, string.Join(", ", funderNameList));

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
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Transaction Type"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Reason Code"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Remark Code"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Transaction Date"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Payments/Adjustments"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "EFT/Check Number"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Payment"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Adjustment"));
            sheetData.AppendChild(headerRow);

            bool isColor = false;
            rowIndex = 1;
            foreach (var data in paymentsAdjustments)
            {
                try
                {

                    Row row = new Row() { RowIndex = (UInt32Value)(uint)(rowIndex + 5) };
                    row.AppendChild(helperService.AddCell(ExcelCellType.character, data.FunderName, isColor));
                    row.AppendChild(helperService.AddCell(ExcelCellType.number, data.ClientId != 0 ? data.ClientId.ToString() : "", isColor));
                    row.AppendChild(helperService.AddCell(ExcelCellType.character, data.ClientFirst, isColor));
                    row.AppendChild(helperService.AddCell(ExcelCellType.character, data.ClientLast, isColor));
                    row.AppendChild(helperService.AddCell(ExcelCellType.date, data.ClaimFrom, isColor));
                    row.AppendChild(helperService.AddCell(ExcelCellType.date, data.ClaimThrough, isColor));
                    row.AppendChild(helperService.AddCell(ExcelCellType.character, data.ClaimStatus, isColor));
                    row.AppendChild(helperService.AddCell(ExcelCellType.date, data.BilledDate != null ? data.BilledDate : null, isColor));
                    row.AppendChild(helperService.AddCell(ExcelCellType.character, data.TransactionType, isColor));
                    row.AppendChild(helperService.AddCell(ExcelCellType.character, data.ReasonCode, isColor));
                    row.AppendChild(helperService.AddCell(ExcelCellType.number, string.IsNullOrWhiteSpace(data.RemarkCode) ? "" : data.RemarkCode.ToString(), isColor));
                    row.AppendChild(helperService.AddCell(ExcelCellType.date, data.TransactionDate, isColor));
                    row.AppendChild(helperService.AddCell(ExcelCellType.date, data.PaymentOrAdjustmentDate, isColor));
                    row.AppendChild(helperService.AddCell(ExcelCellType.character, data.EftOrCheckNumber, isColor));
                    row.AppendChild(helperService.AddCell(ExcelCellType.decimalValue, data.Payment ?? 0, isColor));
                    row.AppendChild(helperService.AddCell(data.Adjustment < 0 ? ExcelCellType.negativeDecimal : ExcelCellType.decimalValue, data.Adjustment, isColor));
                    sheetData.AppendChild(row);
                    rowIndex++;

                    isColor = !isColor;
                }
                catch (Exception) { }

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
    public async Task<ClaimFollowUpResponseModel> GetClaimFollowUpReportAsync(ClaimFollowUpRequestModel model, CancellationToken cancellationToken)
    {
        var result = new ClaimFollowUpResponseModel();

        var (claimFollowUps, total) = await helperService.GetClaimFollowUpReportData(model, cancellationToken);

        result.totalCount = total;
        result.claimFollowUps = claimFollowUps;

        return result;
    }

    public async Task<byte[]> ExportToExcelClaimFollowAsync(ClaimFollowUpRequestModel model, List<ClaimFollowUpResponse> claimFollowUpResponses, CancellationToken cancellationToken)
    {
        // funderNameList
        var funderNameList = await funderNameReportingRepository.Query().AsNoTracking()
                            .Where(x => model.FunderIds.Contains(x.FunderId))
                            .Select(x => x.FunderName)
                            .ToListAsync();


        claimFollowUpResponses = claimFollowUpResponses.ToList();

        var memoryStream = new MemoryStream();
        SpreadsheetDocument document = null;
        try
        {
            document = SpreadsheetDocument.Create(memoryStream, DocumentFormat.OpenXml.SpreadsheetDocumentType.Workbook);
            var workBookPart = document.AddWorkbookPart();
            workBookPart.Workbook = new Workbook();
            var workSheetPart = workBookPart.AddNewPart<WorksheetPart>();

            Columns columns = new Columns();
            columns.Append(new Column() { Min = 1, Max = 21, Width = 13, CustomWidth = true });

            workSheetPart.Worksheet = new Worksheet(columns, new SheetData());
            var sheets = workBookPart.Workbook.AppendChild(new Sheets());
            sheets.AppendChild(new Sheet() { Id = workBookPart.GetIdOfPart(workSheetPart), SheetId = 1, Name = "Claim FollowUP Reports" });

            var sheetData = workSheetPart.Worksheet.GetFirstChild<SheetData>();

            helperService.DefineStyles(workBookPart);

            //AddCustomRows(model, sheetData, 4, string.Join(", ", funderNameList));
            AddClaimFollowCustomRows(model, sheetData, 4, string.Join(", ", funderNameList));

            int rowIndex = 0;
            Row headerRow = new Row() { RowIndex = (UInt32Value)(uint)(rowIndex + 5) };
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Claim Id"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Client First Name"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Client Last Name"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Funder"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Rendering Provider"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Place of Service"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Claim From"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Claim Through"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Authorization"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Expected Amount"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Billed Amount"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Payment Amount"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Adjustment Amount"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Balance"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Billed Date"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Claim Status"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Note"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Note Created By"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Note Created Date"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Follow-up Date"));
            headerRow.AppendChild(helperService.AddCell(ExcelCellType.header, "Follow-up Status"));
            sheetData.AppendChild(headerRow);

            bool isColor = false;
            rowIndex = 1;
            foreach (var data in claimFollowUpResponses)
            {
                try
                {

                    Row row = new Row() { RowIndex = (UInt32Value)(uint)(rowIndex + 5) };
                    row.AppendChild(helperService.AddCell(ExcelCellType.character, data.ClaimId, isColor));
                    row.AppendChild(helperService.AddCell(ExcelCellType.character, data.ClientFirst, isColor));
                    row.AppendChild(helperService.AddCell(ExcelCellType.character, data.ClientLast, isColor));
                    row.AppendChild(helperService.AddCell(ExcelCellType.character, data.FunderName, isColor));
                    row.AppendChild(helperService.AddCell(ExcelCellType.character, data.RenderingProvider, isColor));
                    row.AppendChild(helperService.AddCell(ExcelCellType.character, data.PlaceOfService, isColor));
                    row.AppendChild(helperService.AddCell(ExcelCellType.date, data.ClaimFrom, isColor));
                    row.AppendChild(helperService.AddCell(ExcelCellType.date, data.ClaimThrough, isColor));
                    row.AppendChild(helperService.AddCell(ExcelCellType.character, data.Authorization, isColor));
                    row.AppendChild(helperService.AddCell(ExcelCellType.character, data.ExpectedAmount, isColor));
                    row.AppendChild(helperService.AddCell(ExcelCellType.character, data.BilledAmount, isColor));
                    row.AppendChild(helperService.AddCell(ExcelCellType.character, data.PaymentAmount, isColor));
                    row.AppendChild(helperService.AddCell(ExcelCellType.character, data.AdjustmentAmount, isColor));
                    row.AppendChild(helperService.AddCell(ExcelCellType.character, data.Balance, isColor));
                    row.AppendChild(helperService.AddCell(ExcelCellType.date, data.BilledDate != null ? data.BilledDate : null, isColor));
                    row.AppendChild(helperService.AddCell(ExcelCellType.character, data.ClaimStatus, isColor));
                    row.AppendChild(helperService.AddCell(ExcelCellType.character, data.Note, isColor));
                    row.AppendChild(helperService.AddCell(ExcelCellType.character, data.NoteCreatedByName, isColor));
                    row.AppendChild(helperService.AddCell(ExcelCellType.character, data.NoteCreatedDate, isColor));
                    row.AppendChild(helperService.AddCell(ExcelCellType.character, data.FollowUpDate, isColor));
                    row.AppendChild(helperService.AddCell(ExcelCellType.character, data.FollowUpStatus, isColor));
                    sheetData.AppendChild(row);
                    rowIndex++;

                    isColor = !isColor;
                }
                catch (Exception) { }

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

    private void AddCustomRows(PaymentsAdjustmentsRequestModel model, SheetData sheetData, int numberOfRows, string funderName)
    {
        for (int i = 0; i < numberOfRows; i++)
        {
            var row = new Row();

            if (i == 0)
            {
                row.AppendChild(new Cell() { DataType = CellValues.String, CellValue = new CellValue("Payer/Funder: "), StyleIndex = (int)ExcelCellDesignStyles.fontWithBoldStyle });
                row.AppendChild(new Cell() { DataType = CellValues.String, CellValue = new CellValue(String.Join(", ", funderName)) });
            }
            if (i == 1)
            {
                row.AppendChild(new Cell() { DataType = CellValues.String, CellValue = new CellValue("Transaction Range Type: "), StyleIndex = (int)ExcelCellDesignStyles.fontWithBoldStyle });
                row.AppendChild(new Cell() { DataType = CellValues.String, CellValue = new CellValue(model.RangeType == (int)ReportingDateRangeType.transactionDate ? "Transaction Date" : "Posting Date") });
            }
            if (i == 2)
            {
                row.AppendChild(new Cell() { DataType = CellValues.String, CellValue = new CellValue("Date Range: "), StyleIndex = (int)ExcelCellDesignStyles.fontWithBoldStyle });
                row.AppendChild(new Cell() { DataType = CellValues.String, CellValue = new CellValue(model.StartDate.ToString("MM/dd/yyyy") + " - " + model.EndDate.ToString("MM/dd/yyyy")) });
            }
            sheetData.AppendChild(row);
        }
    }

    private void AddClaimFollowCustomRows(ClaimFollowUpRequestModel model, SheetData sheetData, int numberOfRows, string funderName)
    {
        for (int i = 0; i < numberOfRows; i++)
        {
            var row = new Row();

            if (i == 0)
            {
                row.AppendChild(new Cell() { DataType = CellValues.String, CellValue = new CellValue("Payer/Funder: "), StyleIndex = (int)ExcelCellDesignStyles.fontWithBoldStyle });
                row.AppendChild(new Cell() { DataType = CellValues.String, CellValue = new CellValue(String.Join(", ", funderName)) });
            }
            if (i == 1)
            {
                row.AppendChild(new Cell() { DataType = CellValues.String, CellValue = new CellValue("Follow Up Date Range: "), StyleIndex = (int)ExcelCellDesignStyles.fontWithBoldStyle });
                row.AppendChild(new Cell() { DataType = CellValues.String, CellValue = new CellValue(model.StartDate.ToString("MM/dd/yyyy") + " - " + model.EndDate.ToString("MM/dd/yyyy")) });
            }
            if (i == 2)
            {
                row.AppendChild(new Cell() { DataType = CellValues.String, CellValue = new CellValue("Follow Up Status: "), StyleIndex = (int)ExcelCellDesignStyles.fontWithBoldStyle });
                row.AppendChild(new Cell() { DataType = CellValues.String, CellValue = new CellValue(model.FollowUpType == (int)ReportingClaimFollowUpType.active ? "Active" : "Complete") });
            }
            
            sheetData.AppendChild(row);
        }
    }

    public async Task<int?> FindClaimIdByTransactionTypeIdAsync(ClaimTransactionType transactionType, int transactionTypeId, CancellationToken cancellationToken)
    {
        int? claimId = 0;
        switch (transactionType)
        {
            case ClaimTransactionType.deleteCharge:
                claimId = transactionTypeId;
                break;
            case ClaimTransactionType.writeOff:
                claimId = await helperService.GetClaimIdFromWriteOffIdAsync(transactionTypeId, cancellationToken);
                break;
            case ClaimTransactionType.insurancePayment:
            case ClaimTransactionType.patientPayment:
            case ClaimTransactionType.otherPayment:
            case ClaimTransactionType.eraReceived:
                claimId = await helperService.GetClaimIdFromPaymentIdAsync(transactionTypeId, cancellationToken);
                break;
            case ClaimTransactionType.adjustment:
            case ClaimTransactionType.patientResponsibility:
                claimId = await helperService.GetClaimIdFromAdjustmentIdAsync(transactionTypeId, cancellationToken);
                break;
            case ClaimTransactionType.updatePaymentSummary://TODO
                claimId = transactionTypeId;
                break;
            //TODO:Update Billed date? Status?
            case ClaimTransactionType.deleteClaim:
                //TODO:Delete all PayAdj
                claimId = transactionTypeId;
                break;
            case ClaimTransactionType.deleteChargePayment:
                claimId = await paymentClaimServiceLineRepository.Query().Where(x => x.Id == transactionTypeId).Select(x => x.ClaimChargeEntryId).FirstOrDefaultAsync();
                break;
            case ClaimTransactionType.submitClaim:
            //TODO:Delete all PayAdj except WO
            case ClaimTransactionType.billedAmount:
            case ClaimTransactionType.newDay:
            default:
                break;
        }
        return claimId;
    }

    public async Task<PaymentsAdjustmentsEntity?> GetPaymentsAdjustmentsByIdAsync(ClaimTransactionType transactionType, int claimId, int transactionTypeId, CancellationToken cancellationToken)
    {
        var paymmentsAdjustments = new PaymentsAdjustmentsEntity();

        switch (transactionType)
        {
            case ClaimTransactionType.patientPayment:
            case ClaimTransactionType.otherPayment:
            case ClaimTransactionType.insurancePayment:
            case ClaimTransactionType.eraReceived:
                paymmentsAdjustments = await paymentsAdjustmentsRepository.Query().Where(x => x.TransactionTypeId == transactionTypeId && x.ClaimId == claimId).FirstOrDefaultAsync(cancellationToken);
                break;
            default:
                paymmentsAdjustments = await paymentsAdjustmentsRepository.Query().Where(x => x.TransactionTypeId == transactionTypeId && x.ClaimId == claimId && x.DateDeleted == null).FirstOrDefaultAsync(cancellationToken);
                break;
        }

        return paymmentsAdjustments;
    }

    public async Task<List<PaymentsAdjustmentsEntity?>> GetPaymentsAdjustmentsListByClaimIdAsync(int claimId, CancellationToken cancellationToken)
    {
        return await paymentsAdjustmentsRepository.Query().Where(x => x.ClaimId == claimId && x.DateDeleted == null).ToListAsync();
    }

    public async Task<PaymentsAdjustmentsEntity?> PreparePaymentsAdjustmentsAsync(ClaimTransactionType transactionType, ClaimEntity claim, int transactionTypeId, CancellationToken cancellationToken)
    {
        var paymentsAdjustments = await GetPaymentsAdjustmentsByIdAsync(transactionType, claim.Id, transactionTypeId, cancellationToken);
        paymentsAdjustments ??= new PaymentsAdjustmentsEntity
        {
            ClaimId = claim.Id,
            AccountInfoId = claim.AccountInfoId,
            FunderId = claim.PrimaryFunderId,
            ClientId = claim.ChildProfileId,
            TransactionType = (int)transactionType,
            TransactionTypeId = transactionTypeId,
            DateCreated = EstDateTime,
            PaymentId = await GetPaymentIdAsync(transactionType, transactionTypeId, cancellationToken),
            ChargeEntryId = await GetChargeEntryIdAsync(transactionType, transactionTypeId, cancellationToken) ?? 0,
        };
        paymentsAdjustments.ClaimStatusId = (int)claim.ClaimStatus;
        paymentsAdjustments.BilledDate = claim.billedDate.HasValue ? claim.billedDate : null;
        await SetTransactionTypeValue(paymentsAdjustments, transactionType, cancellationToken);
        await SetClaimDatesAsync(paymentsAdjustments, cancellationToken);

        paymentsAdjustments.DateModified = EstDateTime;
        return paymentsAdjustments;
    }

    public async Task<List<PaymentsAdjustmentsEntity?>> PreparePaymentsAdjustmentsForDeleteAsync(ClaimTransactionType transactionType, int transactionTypeId, int id, CancellationToken cancellationToken)
    {
        var paymentsAdjustmentsList = new List<PaymentsAdjustmentsEntity>();
        switch (transactionType)
        {
            case ClaimTransactionType.deleteChargePayment:
                var paymentId = await paymentClaimServiceLineRepository.Query().Where(x => x.Id == transactionTypeId).Select(x => x.PaymentClaim.PaymentId).FirstOrDefaultAsync(cancellationToken);
                paymentsAdjustmentsList = await paymentsAdjustmentsRepository.Query().Where(x => x.ChargeEntryId == id && x.PaymentId == paymentId && x.DateDeleted == null).ToListAsync(cancellationToken);
                break;
            case ClaimTransactionType.deleteCharge:
                paymentsAdjustmentsList = await paymentsAdjustmentsRepository.Query().Where(x => x.ChargeEntryId == id && x.DateDeleted == null).ToListAsync(cancellationToken);
                break;
            case ClaimTransactionType.deleteClaim:
                paymentsAdjustmentsList = await paymentsAdjustmentsRepository.Query().Where(x => x.ClaimId == id && x.DateDeleted == null).ToListAsync(cancellationToken);
                break;
            default:
                break;
        }
        foreach (var paymentsAdjustments in paymentsAdjustmentsList)
        {
            await SetTransactionTypeValue(paymentsAdjustments, transactionType, cancellationToken);
        }
        return paymentsAdjustmentsList;
    }

    public async Task<int?> GetPaymentIdAsync(ClaimTransactionType transactionType, int transactionTypeId, CancellationToken cancellationToken)
    {
        int? paymentId = 0;
        switch (transactionType)
        {
            case ClaimTransactionType.writeOff:
                paymentId = await helperService.GetPaymentIdFromWriteOffIdAsync(transactionTypeId, cancellationToken);
                break;
            case ClaimTransactionType.insurancePayment:
            case ClaimTransactionType.patientPayment:
            case ClaimTransactionType.otherPayment:
            case ClaimTransactionType.eraReceived:
                paymentId = await helperService.GetPaymentIdFromPaymentIdAsync(transactionTypeId, cancellationToken);
                break;
            case ClaimTransactionType.adjustment:
            case ClaimTransactionType.patientResponsibility:
                paymentId = await helperService.GetPaymentIdFromAdjustmentIdAsync(transactionTypeId, cancellationToken);
                break;
            case ClaimTransactionType.updatePaymentSummary://TODO
            case ClaimTransactionType.deleteCharge:
            case ClaimTransactionType.submitClaim:
            case ClaimTransactionType.deleteClaim:
            case ClaimTransactionType.deleteChargePayment:
            //TODO:Delete all PayAdj except WO
            case ClaimTransactionType.billedAmount:
            case ClaimTransactionType.newDay:
            default:
                break;
        }
        return paymentId;
    }

    public async Task<int?> GetChargeEntryIdAsync(ClaimTransactionType transactionType, int transactionTypeId, CancellationToken cancellationToken)
    {
        int? chargeId = 0;
        switch (transactionType)
        {
            case ClaimTransactionType.writeOff:
                chargeId = await helperService.GetChargeIdFromWriteOffIdAsync(transactionTypeId, cancellationToken);
                break;
            case ClaimTransactionType.insurancePayment:
            case ClaimTransactionType.patientPayment:
            case ClaimTransactionType.otherPayment:
            case ClaimTransactionType.eraReceived:
                chargeId = await helperService.GetChargeIdFromPaymentIdAsync(transactionTypeId, cancellationToken);
                break;
            case ClaimTransactionType.adjustment:
            case ClaimTransactionType.patientResponsibility:
                chargeId = await helperService.GetChargeEntryIdFromAdjustmentIdAsync(transactionTypeId, cancellationToken);
                break;
            case ClaimTransactionType.updatePaymentSummary://TODO
            case ClaimTransactionType.deleteCharge:
            case ClaimTransactionType.submitClaim:
            case ClaimTransactionType.deleteClaim:
            case ClaimTransactionType.deleteChargePayment:
            //TODO:Delete all PayAdj except WO
            case ClaimTransactionType.billedAmount:
            case ClaimTransactionType.newDay:
            default:
                break;
        }
        return chargeId;
    }

    public async Task<PaymentsAdjustmentsEntity?> PreparePaymentsAdjustmentsListAsync(ClaimTransactionType transactionType, PaymentsAdjustmentsEntity paymentsAdjustments, PaymentEntity payment)
    {
        paymentsAdjustments.EftOrCheckNumber = payment.ReferenceNumber;
        paymentsAdjustments.PaymentOrAdjustmentDate = payment.DepositDate;
        paymentsAdjustments.DateModified = EstDateTime;

        return paymentsAdjustments;
    }

    public async Task<ClaimEntity?> GetClaimByIdAsync(int claimId, CancellationToken cancellationToken)
    {
        return await claimRepository.Query().Where(x => x.Id == claimId && x.DateDeleted == null).FirstOrDefaultAsync(cancellationToken);
    }
    public async Task<ClaimChargeEntryEntity?> GetChargeByIdAsync(int chargeId, CancellationToken cancellationToken)
    {
        return await claimChargeEntryRepository.Query().Where(x => x.Id == chargeId).FirstOrDefaultAsync(cancellationToken);
    }

    private async Task SetClaimDatesAsync(PaymentsAdjustmentsEntity paymentsAdjustments, CancellationToken cancellationToken)
    {
        var claimChargeEntries = await helperService.GetChargeEntriesByClaimId(paymentsAdjustments.ClaimId);
        paymentsAdjustments.ClaimFrom = claimChargeEntries.Any() ? claimChargeEntries.OrderBy(x => x.Id).Select(x => x.DateOfService).FirstOrDefault() : paymentsAdjustments.ClaimFrom;
        paymentsAdjustments.ClaimThrough = claimChargeEntries.Any() ? claimChargeEntries.OrderByDescending(x => x.Id).Select(x => x.DateOfService).FirstOrDefault() : paymentsAdjustments.ClaimThrough;
    }

    public async Task<bool> AddPaymentsAdjustmentsAsync(PaymentsAdjustmentsEntity paymentsAdjustmentsEntity, CancellationToken cancellationToken)
    {
        await paymentsAdjustmentsRepository.AddAsync(paymentsAdjustmentsEntity);
        return true;
    }

    public async Task<bool> AddPaymentsAdjustmentsListAsync(List<PaymentsAdjustmentsEntity> paymentsAdjustmentsListEntity, CancellationToken cancellationToken)
    {
        await paymentsAdjustmentsRepository.AddRangeAsync(paymentsAdjustmentsListEntity);
        return true;
    }

    public async Task<int> UpdatePaymentsAdjustmentsAsync(PaymentsAdjustmentsEntity paymentsAdjustmentsEntity, CancellationToken cancellationToken)
    {
        paymentsAdjustmentsRepository.Update(paymentsAdjustmentsEntity);
        return 1;
    }

    public async Task<int> UpdatePaymentsAdjustmentListsAsync(List<PaymentsAdjustmentsEntity> paymentsAdjustmentsEntity, CancellationToken cancellationToken)
    {
        paymentsAdjustmentsRepository.UpdateRange(paymentsAdjustmentsEntity);
        return 1;
    }

    private async Task CommitPaymentsAdjustmentsAsync()
    {
        await paymentsAdjustmentsRepository.CommitAsync();
    }

    private async Task<PaymentsAdjustmentsEntity> SetTransactionTypeValue(PaymentsAdjustmentsEntity paymentsAdjustments, ClaimTransactionType transactionType, CancellationToken cancellationToken)
    {
        var payment = paymentsAdjustments.PaymentId != null || paymentsAdjustments.PaymentId != 0 ? await GetPaymentAsync(paymentsAdjustments.PaymentId.Value) : null;
        switch (transactionType)
        {
            case ClaimTransactionType.insurancePayment:
            case ClaimTransactionType.patientPayment:
            case ClaimTransactionType.otherPayment:
            case ClaimTransactionType.eraReceived:
                await GetPaymentOrAdjustmentDetailsAsync(paymentsAdjustments, transactionType);
                paymentsAdjustments.PaymentOrAdjustmentDate = payment != null ? payment.DepositDate : null;
                paymentsAdjustments.EftOrCheckNumber = payment != null ? payment.ReferenceNumber : null;
                break;
            case ClaimTransactionType.adjustment:
            case ClaimTransactionType.patientResponsibility:
                await GetPaymentOrAdjustmentDetailsAsync(paymentsAdjustments, transactionType);
                paymentsAdjustments.PaymentOrAdjustmentDate = payment != null ? payment.DepositDate : null;
                paymentsAdjustments.EftOrCheckNumber = payment != null ? payment.ReferenceNumber : null;

                break;
            case ClaimTransactionType.writeOff:
                await GetPaymentOrAdjustmentDetailsAsync(paymentsAdjustments, transactionType);
                break;
            case ClaimTransactionType.deleteClaim://MULTIPLE ENTRIES WILL NEED TO BE DELETED
            case ClaimTransactionType.deleteCharge://PAYMENTS, ADJUSTMENTS & WO TO BE DELETED FOR SPECIFIC CHARGE
            case ClaimTransactionType.deleteChargePayment://PAYMENTS & ADJUSTMENTS FOR SPECIFIC CHARGE
                paymentsAdjustments.DateDeleted = EstDateTime;
                break;
            case ClaimTransactionType.billedAmount:
            case ClaimTransactionType.updatePaymentSummary://TODO
            case ClaimTransactionType.newDay://TODO
            case ClaimTransactionType.submitClaim://TODO
            default:
                break;
        }
        return paymentsAdjustments;
    }

    public async Task<PaymentEntity?> GetPaymentAsync(int paymentId)
    {
        var payment = paymentId != 0 ? await paymentRepository.Query().Where(x => x.Id == paymentId && x.DateDeleted == null).FirstOrDefaultAsync() : null;
        return payment;
    }

    private async Task GetAndSetAdjustmentDetailsAsync(PaymentsAdjustmentsEntity paymentsAdjustments, ClaimTransactionType adjustmentTransactionType)
    {
        var serviceLineAdjustment = await paymentClaimServiceLineAdjustmentRepository.Query().Where(x => x.Id == paymentsAdjustments.TransactionTypeId && (IsAdjustmentTypePR(adjustmentTransactionType) ? (x.AdjustmentGroupCode == prAdjustmentGroupCode) : (x.AdjustmentGroupCode != prAdjustmentGroupCode)) && x.DateDeleted == null)
                                              .Select(x => new { amount = x.AdjustmentAmount, x.IsAdjustmentPositive, reasonCode = x.AdjustmentGroupCode, remarkCode = x.AdjustmentReasonCode, x.DateCreated, x.DateLastModified }).FirstOrDefaultAsync();
        if (serviceLineAdjustment != null)
        {
            var adjustment = serviceLineAdjustment.IsAdjustmentPositive == true ? serviceLineAdjustment.amount : -serviceLineAdjustment.amount;
            paymentsAdjustments.Adjustment = adjustment ?? 0;
            paymentsAdjustments.ReasonCode = serviceLineAdjustment.reasonCode;
            paymentsAdjustments.RemarkCode = string.IsNullOrWhiteSpace(serviceLineAdjustment.remarkCode) ? null : serviceLineAdjustment.remarkCode;
            paymentsAdjustments.TransactionDate = paymentsAdjustments.TransactionDate == null ? serviceLineAdjustment.DateCreated : serviceLineAdjustment.DateLastModified;
            paymentsAdjustments.EftOrCheckNumber = "";//TODO
        }
        paymentsAdjustments.DateDeleted = serviceLineAdjustment != null ? null : EstDateTime;
    }

    private async Task GetAndSetPaymentDetailsAsync(PaymentsAdjustmentsEntity paymentsAdjustments, int paymentTypeId)
    {
        var payment = await paymentClaimServiceLineRepository.Query().Where(x => x.Id == paymentsAdjustments.TransactionTypeId && x.DateDeleted == null).Select(x => new { x.PaymentAmount, x.DateCreated, x.DateLastModified }).FirstOrDefaultAsync();
        if (payment.PaymentAmount != null && payment.PaymentAmount != 0)
        {
            paymentsAdjustments.Payment = payment?.PaymentAmount ?? 0;
            paymentsAdjustments.DateDeleted = null;
        }
        else
        {
            paymentsAdjustments.DateDeleted = EstDateTime;
        }
        paymentsAdjustments.TransactionDate = payment?.DateLastModified;
        paymentsAdjustments.RemarkCode = "";//TODO
        paymentsAdjustments.ReasonCode = paymentTypeId.ToString();//TODO: Insurance/Patient/Other
    }

    private async Task GetPaymentOrAdjustmentDetailsAsync(PaymentsAdjustmentsEntity paymentsAdjustments, ClaimTransactionType transactionType)
    {
        switch (transactionType)
        {
            case ClaimTransactionType.writeOff:
                var writeOffAmount = await claimChargeEntryWriteOffRepository.Query().Where(x => x.Id == paymentsAdjustments.TransactionTypeId && x.DateDeleted == null).Select(x => new { amount = x.WriteOffAmount, x.DateCreated, x.DateLastModified }).FirstOrDefaultAsync();
                if (writeOffAmount != null && writeOffAmount.amount != 0)
                {
                    paymentsAdjustments.Adjustment = -writeOffAmount?.amount ?? 0;
                    paymentsAdjustments.TransactionDate = paymentsAdjustments.TransactionDate == null ? writeOffAmount?.DateCreated : writeOffAmount?.DateLastModified;
                    paymentsAdjustments.PaymentOrAdjustmentDate = paymentsAdjustments.PaymentOrAdjustmentDate == null ? writeOffAmount?.DateCreated : writeOffAmount?.DateLastModified;
                    paymentsAdjustments.EftOrCheckNumber = "";//TODO
                    paymentsAdjustments.RemarkCode = "";//TODO
                    paymentsAdjustments.ReasonCode = "WO";//TODO
                }
                else
                {
                    paymentsAdjustments.DateDeleted = EstDateTime;
                }
                break;
            case ClaimTransactionType.insurancePayment:
            case ClaimTransactionType.patientPayment:
            case ClaimTransactionType.otherPayment:
            case ClaimTransactionType.eraReceived:
                await GetAndSetPaymentDetailsAsync(paymentsAdjustments, FindPaymentTypeId(transactionType));
                break;
            case ClaimTransactionType.adjustment:
            case ClaimTransactionType.patientResponsibility:
                await GetAndSetAdjustmentDetailsAsync(paymentsAdjustments, transactionType);
                break;
            case ClaimTransactionType.billedAmount:
            default:
                break;
        }
    }

}
