using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models.ReportingModels;
using SummationService.Domain.Interfaces;
using System.Data;
using System.Data.Common;

namespace SummationService.Domain.Services
{
    public class MonthlyFinancialSummaryService : IMonthlyFinancialSummaryService
    {
        private readonly IDbHelper<BillingDbContext> _dbHelper;
        private readonly IHelperService _helperService;
        private readonly IRepository<BillingDbContext, ClaimEntity> _claimRepository;
        private readonly IRepository<BillingDbContext, ClaimSearchFunderEntity> _funderSearchRepository;

        public MonthlyFinancialSummaryService(IDbHelper<BillingDbContext> dbHelper, IHelperService helperService, IRepository<BillingDbContext, ClaimEntity> clientRepository,IRepository<BillingDbContext, ClaimSearchFunderEntity> funderSearchRepository)
        {
            _dbHelper = dbHelper;
            _helperService = helperService;
            _claimRepository = clientRepository;
            _funderSearchRepository = funderSearchRepository;
        }

        public async Task<MonthlyFinancialSummaryResponse> GetMonthlyFinancialSummaryAsync(
            int AccountInfoId,
            DateTime startDate,
            DateTime endDate,
            string dateType = "Transaction",
            IEnumerable<int>? locationIds = null,
            IEnumerable<int>? funderIds = null)
        {
            var sqlParams = new List<SqlParameter>
            {
                new("@AccountInfoId", SqlDbType.Int) { Value = AccountInfoId },
                new("@StartDate", SqlDbType.Date) { Value = startDate },
                new("@EndDate", SqlDbType.Date) { Value = endDate },
                new("@DateBasis", SqlDbType.NVarChar, 20) { Value = dateType },
                // Optional filters: pass as CSV string or DBNull.Value if null/empty
                new("@LocationIds", SqlDbType.NVarChar, -1)
                {
                    Value = (locationIds != null && locationIds.Any())
                            ? string.Join(",", locationIds)
                            : (object)DBNull.Value
                },
                new("@FunderIds", SqlDbType.NVarChar, -1)
                {
                    Value = (funderIds != null && funderIds.Any())
                            ? string.Join(",", funderIds)
                            : (object)DBNull.Value
                }
            };

            var result = new MonthlyFinancialSummaryResponse
            {
                DateBasis = dateType
            };

            using var reader = await _dbHelper.ExecuteReaderAsync("dbo.usp_MonthlyFinancialSummary", sqlParams);

            /* ==============================
               Result Set 1: Starting AR
               ============================== */
            if (await reader.ReadAsync())
            {
                result.StartingAR = reader.GetDecimal(0);
            }

            /* ==============================
               Result Set 2: Monthly Rows + TOTAL
               ============================== */
            await reader.NextResultAsync();

            while (await reader.ReadAsync())
            {
                var row = MapMonthlyFinancialRow(reader);

                if (string.Equals(row.MonthYear, "TOTAL", StringComparison.OrdinalIgnoreCase))
                {
                    result.Total = row;
                }
                else
                {
                    result.Rows.Add(row);
                }
            }

            // Result Set 3: Unapplied Credits
            await reader.NextResultAsync();

            if (await reader.ReadAsync())
            {
                result.UnappliedCredits = new UnappliedCreditsDto
                {
                    InsuranceUnapplied = reader.GetDecimal(reader.GetOrdinal("InsuranceUnapplied")),
                    PatientUnapplied = reader.GetDecimal(reader.GetOrdinal("PatientUnapplied")),
                    TotalUnapplied = reader.GetDecimal(reader.GetOrdinal("TotalUnapplied"))
                };
            }

            return result;
        }

        private static MonthlyFinancialRow MapMonthlyFinancialRow(DbDataReader reader)
        {
            return new MonthlyFinancialRow
            {
                MonthYear = reader["MonthYear"].ToString()!,
                Charges = reader.GetDecimal(reader.GetOrdinal("Charges")),
                InsurancePay = reader.GetDecimal(reader.GetOrdinal("InsurancePay")),
                PatientPay = reader.GetDecimal(reader.GetOrdinal("PatientPay")),
                TotalPay = reader.GetDecimal(reader.GetOrdinal("TotalPay")),
                Adjustments = reader.GetDecimal(reader.GetOrdinal("Adjustments")),
                WriteOffs = reader.GetDecimal(reader.GetOrdinal("WriteOffs")),
                PeriodBalance = reader.GetDecimal(reader.GetOrdinal("PeriodBalance")),
                EndingAR = reader.GetDecimal(reader.GetOrdinal("EndingAR"))
            };
        }

        public async Task<byte[]> ExportToExcelAsync(MonthlyFinancialSummaryRequest model, MonthlyFinancialSummaryResponse response,CancellationToken cancellationToken)
        {
            var memoryStream = new MemoryStream();
            SpreadsheetDocument document = null;

            try
            {
                document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);

                var workBookPart = document.AddWorkbookPart();
                workBookPart.Workbook = new Workbook();

                var workSheetPart = workBookPart.AddNewPart<WorksheetPart>();

                Columns columns = new Columns();

                // Column A
                columns.Append(new Column { Min = 1, Max = 1, Width = 30, CustomWidth = true });

                // Column B
                columns.Append(new Column { Min = 2, Max = 2, Width = 28, CustomWidth = true });

                // Column C–H
                columns.Append(new Column { Min = 3, Max = 8, Width = 15, CustomWidth = true });

                // Column I
                columns.Append(new Column { Min = 9, Max = 9, Width = 28, CustomWidth = true });

                workSheetPart.Worksheet = new Worksheet(columns, new SheetData());

                var sheets = workBookPart.Workbook.AppendChild(new Sheets());
                sheets.AppendChild(new Sheet
                {
                    Id = workBookPart.GetIdOfPart(workSheetPart),
                    SheetId = 1,
                    Name = "Monthly Financial Summary"
                });

                var sheetData = workSheetPart.Worksheet.GetFirstChild<SheetData>();

                _helperService.DefineStyles(workBookPart);

                // =========================
                // Build Filter Values (Names)
                // =========================
                string locationText = "All";

                if (model.LocationNames != null)
                {
                    locationText = string.Join(", ", model.LocationNames);   
                }

                string funderText = "All";
                if (model.FunderIds != null && model.FunderIds.Any())
                {
                    var funderInfos = await GetFunderInfoByIds(model.AccountInfoId);

                    var funderNames = funderInfos
                        .Where(f => model.FunderIds.Contains(f.Id))
                        .Select(f => f.Name)
                        .ToList();

                    funderText = string.Join(", ", funderNames);
                }

                string dateTypeText = model.DateType ?? "Transaction";

                // =========================
                // Custom Header (EACH FILTER IN ONE ROW)
                // =========================
                AddCustomRows(
                    model,
                    sheetData,
                    4,
                    dateTypeText,
                    locationText,
                    funderText
                );

                // After 4 filter rows, start table at Row 6
                uint rowIndex = 6;

                /* =========================
                   Header Row
                   ========================= */
                Row headerRow = new Row { RowIndex = rowIndex };
                headerRow.Append(
                    _helperService.AddCell(ExcelCellType.header, "Month-Year"),
                    _helperService.AddCell(ExcelCellType.header, "Charges ($)"),
                    _helperService.AddCell(ExcelCellType.header, "Insurance Pay ($)"),
                    _helperService.AddCell(ExcelCellType.header, "Patient Pay ($)"),
                    _helperService.AddCell(ExcelCellType.header, "Total Pay ($)"),
                    _helperService.AddCell(ExcelCellType.header, "Adjustments ($)"),
                    _helperService.AddCell(ExcelCellType.header, "WriteOff ($)"),
                    _helperService.AddCell(ExcelCellType.header, "Period Balance ($)"),
                    _helperService.AddCell(ExcelCellType.header, "Total Balance ($)")
                );
                sheetData.AppendChild(headerRow);

                /* =========================
                   Previous Period Row
                   ========================= */
                rowIndex++;
                Row previousRow = new Row { RowIndex = rowIndex };
                previousRow.Append(
                    _helperService.AddCell(ExcelCellType.character, "Previous Period"),
                    _helperService.AddCell(ExcelCellType.character, ""),
                    _helperService.AddCell(ExcelCellType.character, ""),
                    _helperService.AddCell(ExcelCellType.character, ""),
                    _helperService.AddCell(ExcelCellType.character, ""),
                    _helperService.AddCell(ExcelCellType.character, ""),
                    _helperService.AddCell(ExcelCellType.character, ""),
                    _helperService.AddCell(ExcelCellType.character, ""),
                    _helperService.AddCell(ExcelCellType.decimalValue, response.StartingAR)
                );
                sheetData.AppendChild(previousRow);

                /* =========================
                   Monthly Rows
                   ========================= */
                bool isColor = false;

                foreach (var rowData in response.Rows)
                {
                    rowIndex++;

                    Row row = new Row { RowIndex = rowIndex };
                    row.Append(
                        _helperService.AddCell(ExcelCellType.character, rowData.MonthYear, isColor),
                        _helperService.AddCell(ExcelCellType.decimalValue, rowData.Charges, isColor),
                        _helperService.AddCell(ExcelCellType.negativeDecimal, rowData.InsurancePay, isColor),
                        _helperService.AddCell(ExcelCellType.negativeDecimal, rowData.PatientPay, isColor),
                        _helperService.AddCell(ExcelCellType.negativeDecimal, rowData.TotalPay, isColor),
                        _helperService.AddCell(ExcelCellType.negativeDecimal, rowData.Adjustments, isColor),
                        _helperService.AddCell(ExcelCellType.decimalValue, rowData.WriteOffs, isColor),
                        _helperService.AddCell(ExcelCellType.negativeDecimal, rowData.PeriodBalance, isColor),
                        _helperService.AddCell(ExcelCellType.decimalValue, rowData.EndingAR, isColor)
                    );

                    sheetData.AppendChild(row);
                    isColor = !isColor;
                }

                /* =========================
                   TOTAL Row
                   ========================= */
                if (response.Total != null)
                {
                    rowIndex++;
                    Row blankRow = new Row { RowIndex = rowIndex };
                    sheetData.AppendChild(blankRow);

                    rowIndex++;
                    Row totalRow = new Row { RowIndex = rowIndex };
                    totalRow.Append(
                        _helperService.AddCell(ExcelCellType.header, "TOTAL"),
                        _helperService.AddCell(ExcelCellType.decimalValue, response.Total.Charges),
                        _helperService.AddCell(ExcelCellType.negativeDecimal, response.Total.InsurancePay),
                        _helperService.AddCell(ExcelCellType.negativeDecimal, response.Total.PatientPay),
                        _helperService.AddCell(ExcelCellType.negativeDecimal, response.Total.TotalPay),
                        _helperService.AddCell(ExcelCellType.negativeDecimal, response.Total.Adjustments),
                        _helperService.AddCell(ExcelCellType.decimalValue, response.Total.WriteOffs),
                        _helperService.AddCell(ExcelCellType.negativeDecimal, response.Total.PeriodBalance),
                        _helperService.AddCell(ExcelCellType.decimalValue, response.Total.EndingAR)
                    );
                    sheetData.AppendChild(totalRow);
                }

                /* =========================
                   Unapplied Credits Block
                   ========================= */
                if (response.UnappliedCredits != null)
                {
                    rowIndex += 2;

                    Row insuranceRow = new Row { RowIndex = rowIndex };
                    insuranceRow.Append(
                        _helperService.AddCell(ExcelCellType.header, "Insurance Unapplied Credit"),
                        _helperService.AddCell(ExcelCellType.decimalValue, response.UnappliedCredits.InsuranceUnapplied)
                    );
                    sheetData.AppendChild(insuranceRow);

                    rowIndex++;
                    Row patientRow = new Row { RowIndex = rowIndex };
                    patientRow.Append(
                        _helperService.AddCell(ExcelCellType.header, "Patient Unapplied Credit"),
                        _helperService.AddCell(ExcelCellType.decimalValue, response.UnappliedCredits.PatientUnapplied)
                    );
                    sheetData.AppendChild(patientRow);

                    rowIndex++;
                    Row totalUnappliedRow = new Row { RowIndex = rowIndex };
                    totalUnappliedRow.Append(
                        _helperService.AddCell(ExcelCellType.header, "Total Unapplied Credit"),
                        _helperService.AddCell(ExcelCellType.decimalValue, response.UnappliedCredits.TotalUnapplied)
                    );
                    sheetData.AppendChild(totalUnappliedRow);
                }

                workBookPart.Workbook.Save();
            }
            catch (Exception ex)
            {
                throw new Exception(
                    "An error occurred while generating Monthly Financial Summary Excel.", ex);
            }
            finally
            {
                document?.Dispose();
            }

            return memoryStream.ToArray();
        }

        private void AddCustomRows(MonthlyFinancialSummaryRequest model, SheetData sheetData, int numberOfRows, string dateTypeText, string locationText, string funderText)
        {
            for (int i = 0; i < numberOfRows; i++)
            {
                var row = new Row();

                if (i == 0)
                {
                    row.AppendChild(new Cell()
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue("Report Type:"),
                        StyleIndex = (int)ExcelCellDesignStyles.fontWithBoldStyle
                    });
                    row.AppendChild(new Cell()
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue("Monthly")
                    });
                }
                else if (i == 1)
                {
                    row.AppendChild(new Cell()
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue("Date Range:"),
                        StyleIndex = (int)ExcelCellDesignStyles.fontWithBoldStyle
                    });
                    row.AppendChild(new Cell()
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue($"{model.StartDate:MM/dd/yyyy} - {model.EndDate:MM/dd/yyyy}")
                    });
                }
                else if (i == 2)
                {
                    row.AppendChild(new Cell()
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue("Location:"),
                        StyleIndex = (int)ExcelCellDesignStyles.fontWithBoldStyle
                    });
                    row.AppendChild(new Cell()
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue(locationText)
                    });
                }
                else if (i == 3)
                {
                    row.AppendChild(new Cell()
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue("Funder:"),
                        StyleIndex = (int)ExcelCellDesignStyles.fontWithBoldStyle
                    });
                    row.AppendChild(new Cell()
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue(funderText)
                    });
                    sheetData.AppendChild(row);
                    sheetData.AppendChild(new Row());

                    continue;
                }

                sheetData.AppendChild(row);
            }
        }

        public async Task<List<BaseNameOption>> GetFunderInfoByIds(int accountInfoId)
        {
            var funderIds = await _claimRepository
                                 .Query()
                                 .Where(x => x.AccountInfoId == accountInfoId
                                             && (x.ClaimStatus == ClaimStatus.PendingReview || x.ClaimStatus == ClaimStatus.ReadyToBill)
                                             && x.ClaimAppointmentLinks.Any()).Select(x => x.PrimaryFunderId).Distinct()
                                 .ToListAsync();

            var result = await _funderSearchRepository
                                .Query()
                                .Where(c => funderIds.Contains(c.Id))
                                .Select(c => new BaseNameOption { Id = c.Id, Name = c.Name })
                                .ToListAsync();
            return result;
        }


         public class BaseNameOption
        {
           public int Id { get; set; }
           public string Name { get; set; } = string.Empty;
        }

    }
}
