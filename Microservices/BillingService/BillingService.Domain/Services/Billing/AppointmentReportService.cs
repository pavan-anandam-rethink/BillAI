using AutoMapper;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Scheduling;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.ReportingModels;
using Rethink.Services.Common.Services;
using Rethink.Services.Common.Utils;
using SummationService.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Billing
{
    public class AppointmentReportService : BaseService, IAppointmentReportService
    {
        private readonly IRepository<BillingDbContext, ClaimAppointmentLinkEntity> _linkRepository;
        private readonly IRepository<BillingDbContext, ClaimEntity> _claimRepository;
        private readonly IRepository<BillingDbContext, AppointmentClaimProcessingErrorEntity> _appointmentClaimProcessingErrorRepository;
        private readonly IAppointmentService _appointmentService;
        private readonly IRethinkMasterDataMicroServices _rethinkService;
        private readonly IHelperService _helperService;
        private readonly IMapper _mapper;
        private readonly int unProcessedAppointmentDaysThreshold = -30;
        private readonly IClaimSyncService _claimSyncService;
        private readonly IRepository<BillingDbContext, UnProcessedApointmentScheduleEntity> _unProcessedApointmentScheduleRepository;

        public AppointmentReportService(
            IRepository<BillingDbContext, ClaimAppointmentLinkEntity> linkRepository,
            IRepository<BillingDbContext, ClaimEntity> claimRepository,
            IRepository<BillingDbContext, AppointmentClaimProcessingErrorEntity> appointmentClaimProcessingErrorRepository,
            IAppointmentService appointmentService,
            IRethinkMasterDataMicroServices rethinkService,
            IHelperService helperService,
            IClaimSyncService claimSyncService,
            IMapper mapper,
            IRepository<BillingDbContext, UnProcessedApointmentScheduleEntity> unProcessedApointmentScheduleRepository)
        {
            _linkRepository = linkRepository;
            _claimRepository = claimRepository;
            _appointmentClaimProcessingErrorRepository = appointmentClaimProcessingErrorRepository;
            _appointmentService = appointmentService;
            _rethinkService = rethinkService;
            _helperService = helperService;
            _claimSyncService = claimSyncService;
            _mapper = mapper;
            _unProcessedApointmentScheduleRepository = unProcessedApointmentScheduleRepository;
        }

        public async Task<AppointmentModelWithCount> GetUnbilledAppointmentDetails(UnbilledAppointmentsRequestModel model)
        {
            var unbilledAppointments = new AppointmentModelWithCount();

            var appointmentList = await GetUnbilledData(model);

            if (appointmentList.Count == 0)
            {
                unbilledAppointments.appointmentModels = new List<AppointmentModel>();
                return unbilledAppointments;
            }

            var count = appointmentList.Count;
            if (model.Take != 0)
                appointmentList = appointmentList.Skip(model.Skip).Take(model.Take).ToList();

            await _appointmentService.SetupRethinkDataForAppointments(appointmentList);
            var mappedAppointmentDetails = await _appointmentService.ToAppointmentItems(model.AccountInfoId, appointmentList, model.MemberId);
            var apptMappedDetails = mappedAppointmentDetails.AsQueryable();

            if (model.SortingModels != null && model.SortingModels.Count > 0)
            {
                apptMappedDetails = apptMappedDetails.OrderBy(model.SortingModels);
            }

            unbilledAppointments.appointmentModels = apptMappedDetails.ToList();
            unbilledAppointments.totalCount = count;

            return unbilledAppointments;
        }

        public async Task<bool> CreateClaimsForUnbilledAppointmentsAsync(int accountInfoId, int memberId, int[] apptId)
        {
            foreach (int appointmentId in apptId)
            {
                await _claimSyncService.PublishUnbilledAppointmentForClaimProcessingAsync(accountInfoId, memberId, appointmentId);
            }
            return true;
        }

        public async Task<byte[]> ExportUnbilledAppointmentDataAsync(ExportModelForUnbilledAppointments exportModel)
        {
            var memoryStream = new MemoryStream();
            SpreadsheetDocument document = null;
            var model = exportModel.Model;
            try
            {
                var unbilledData = await GetUnbilledData(model);
                unbilledData = await _appointmentService.SetupRethinkDataForAppointments(unbilledData);
                var mappedAppointmentDetails = await _appointmentService.ToAppointmentItems(model.AccountInfoId, unbilledData, model.MemberId);

                document = SpreadsheetDocument.Create(memoryStream, DocumentFormat.OpenXml.SpreadsheetDocumentType.Workbook);
                var workBookPart = document.AddWorkbookPart();
                workBookPart.Workbook = new Workbook();
                var workSheetPart = workBookPart.AddNewPart<WorksheetPart>();

                Columns columns = new();
                columns.Append(new Column() { Min = 1, Max = 18, Width = 13, CustomWidth = true });

                workSheetPart.Worksheet = new Worksheet(columns, new SheetData());
                var sheets = workBookPart.Workbook.AppendChild(new Sheets());
                sheets.AppendChild(new Sheet() { Id = workBookPart.GetIdOfPart(workSheetPart), SheetId = 1, Name = "Unbilled Appointment Reports" });

                var sheetData = workSheetPart.Worksheet.GetFirstChild<SheetData>();

                _helperService.DefineStyles(workBookPart);

                AddCustomRows(exportModel.Filter, sheetData);

                int rowIndex = 0;
                Row headerRow = new() { RowIndex = (UInt32Value)(uint)(rowIndex + 10) };
                headerRow.AppendChild(_helperService.AddCell(ExcelCellType.header, "Appointment Id"));
                headerRow.AppendChild(_helperService.AddCell(ExcelCellType.header, "Client"));
                headerRow.AppendChild(_helperService.AddCell(ExcelCellType.header, "Payer/Funder"));
                headerRow.AppendChild(_helperService.AddCell(ExcelCellType.header, "Date of Service"));
                headerRow.AppendChild(_helperService.AddCell(ExcelCellType.header, "Staff"));
                headerRow.AppendChild(_helperService.AddCell(ExcelCellType.header, "Start Time"));
                headerRow.AppendChild(_helperService.AddCell(ExcelCellType.header, "End Time"));
                headerRow.AppendChild(_helperService.AddCell(ExcelCellType.header, "Service"));
                headerRow.AppendChild(_helperService.AddCell(ExcelCellType.header, "Billing Code"));
                headerRow.AppendChild(_helperService.AddCell(ExcelCellType.header, "Location"));
                headerRow.AppendChild(_helperService.AddCell(ExcelCellType.header, "Place of Service"));
                sheetData.AppendChild(headerRow);

                bool isColor = false;
                rowIndex = 1;
                foreach (var data in mappedAppointmentDetails)
                {
                    Row row = new() { RowIndex = (UInt32Value)(uint)(rowIndex + 10) };
                    row.AppendChild(_helperService.AddCell(ExcelCellType.character, data.Id, isColor));
                    row.AppendChild(_helperService.AddCell(ExcelCellType.character, data.ClientName.ToString(), isColor));
                    row.AppendChild(_helperService.AddCell(ExcelCellType.character, data.FunderName, isColor));
                    row.AppendChild(_helperService.AddCell(ExcelCellType.date, data.StartDate, isColor));
                    row.AppendChild(_helperService.AddCell(ExcelCellType.character, data.StaffName, isColor));
                    row.AppendChild(_helperService.AddCell(ExcelCellType.character, data.StartDate.ToString("hh:mm tt"), isColor));
                    row.AppendChild(_helperService.AddCell(ExcelCellType.character, data.EndDate.ToString("hh:mm tt"), isColor));
                    row.AppendChild(_helperService.AddCell(ExcelCellType.character, data.ServiceName, isColor));
                    row.AppendChild(_helperService.AddCell(ExcelCellType.character, data.BillingCode, isColor));
                    row.AppendChild(_helperService.AddCell(ExcelCellType.character, data.ServiceLocation, isColor));
                    row.AppendChild(_helperService.AddCell(ExcelCellType.character, data.Location, isColor));

                    sheetData.AppendChild(row);
                    rowIndex++;

                    isColor = !isColor;
                }
                workBookPart.Workbook.Save();

            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while generating the excel file.", ex);

            }
            finally
            {
                document?.Dispose();
            }
            return memoryStream.ToArray();
        }

        public async Task<byte[]> ExportUnprocessedAppointmentDataAsync(ExportModelForUnprocessedAppointments exportModel)
        {
            var memoryStream = new MemoryStream();
            SpreadsheetDocument document = null;
            var model = _mapper.Map<UnprocessedAppointmentsRequestModel>(exportModel.Model);
            try
            {
                var unprocessedAppointmentsWithCount = await UnprocessedAppointments(model);

                document = SpreadsheetDocument.Create(memoryStream, DocumentFormat.OpenXml.SpreadsheetDocumentType.Workbook);
                var workBookPart = document.AddWorkbookPart();
                workBookPart.Workbook = new Workbook();
                var workSheetPart = workBookPart.AddNewPart<WorksheetPart>();

                Columns columns = new();
                columns.Append(new Column() { Min = 1, Max = 18, Width = 13, CustomWidth = true });

                workSheetPart.Worksheet = new Worksheet(columns, new SheetData());
                var sheets = workBookPart.Workbook.AppendChild(new Sheets());
                sheets.AppendChild(new Sheet() { Id = workBookPart.GetIdOfPart(workSheetPart), SheetId = 1, Name = "Unprocessed Appointment Reports" });

                var sheetData = workSheetPart.Worksheet.GetFirstChild<SheetData>();

                _helperService.DefineStyles(workBookPart);

                AddCustomRows(exportModel.Filter, sheetData);

                int rowIndex = 0;
                Row headerRow = new() { RowIndex = (UInt32Value)(uint)(rowIndex + 10) };
                headerRow.AppendChild(_helperService.AddCell(ExcelCellType.header, "Appointment Id"));
                headerRow.AppendChild(_helperService.AddCell(ExcelCellType.header, "Client"));
                headerRow.AppendChild(_helperService.AddCell(ExcelCellType.header, "Payer/Funder"));
                headerRow.AppendChild(_helperService.AddCell(ExcelCellType.header, "Date of Service"));
                headerRow.AppendChild(_helperService.AddCell(ExcelCellType.header, "Staff"));
                headerRow.AppendChild(_helperService.AddCell(ExcelCellType.header, "Start Time"));
                headerRow.AppendChild(_helperService.AddCell(ExcelCellType.header, "End Time"));
                headerRow.AppendChild(_helperService.AddCell(ExcelCellType.header, "Service"));
                headerRow.AppendChild(_helperService.AddCell(ExcelCellType.header, "Billing Code"));
                headerRow.AppendChild(_helperService.AddCell(ExcelCellType.header, "Location"));
                headerRow.AppendChild(_helperService.AddCell(ExcelCellType.header, "Place of Service"));
                headerRow.AppendChild(_helperService.AddCell(ExcelCellType.header, "Error"));
                sheetData.AppendChild(headerRow);

                bool isColor = false;
                rowIndex = 1;
                foreach (var data in unprocessedAppointmentsWithCount.appointmentModels)
                {
                    Row row = new() { RowIndex = (UInt32Value)(uint)(rowIndex + 10) };
                    row.AppendChild(_helperService.AddCell(ExcelCellType.character, data.Id, isColor));
                    row.AppendChild(_helperService.AddCell(ExcelCellType.character, data.ClientName.ToString(), isColor));
                    row.AppendChild(_helperService.AddCell(ExcelCellType.character, data.FunderName, isColor));
                    row.AppendChild(_helperService.AddCell(ExcelCellType.date, data.StartDate, isColor));
                    row.AppendChild(_helperService.AddCell(ExcelCellType.character, data.StaffName, isColor));
                    row.AppendChild(_helperService.AddCell(ExcelCellType.character, data.StartDate.ToString("hh:mm tt"), isColor));
                    row.AppendChild(_helperService.AddCell(ExcelCellType.character, data.EndDate.ToString("hh:mm tt"), isColor));
                    row.AppendChild(_helperService.AddCell(ExcelCellType.character, data.ServiceName, isColor));
                    row.AppendChild(_helperService.AddCell(ExcelCellType.character, data.BillingCode, isColor));
                    row.AppendChild(_helperService.AddCell(ExcelCellType.character, data.ServiceLocation, isColor));
                    row.AppendChild(_helperService.AddCell(ExcelCellType.character, data.Location, isColor));
                    row.AppendChild(_helperService.AddCell(ExcelCellType.character, data.appointmentErrorMessage ?? string.Empty, isColor));

                    sheetData.AppendChild(row);
                    rowIndex++;

                    isColor = !isColor;
                }
                workBookPart.Workbook.Save();
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while generating the excel file.", ex);
            }
            finally
            {
                document?.Dispose();
            }
            return memoryStream.ToArray();
        }

        public async Task<int> UnprocessedAppointmentsCountAsync(int accountInfoId)
        {
            var unProcessAppointments = await _linkRepository.Query()
            .Include(c => c.AppointmentClaimProcessingError).AsNoTracking()
            .Where(x => x.ClaimId == 0 && x.DateDeleted == null && x.AccountInfoId == accountInfoId)
            .Select(x => new
            {
                x.AppointmentId,
                x.AppointmentClaimProcessingError.ErrorMessage
            }).ToListAsync();

            // Get Appointment with claims
            var processedAppointmentIds = await _linkRepository.Query()
                .Where(x => x.ClaimId != 0 && x.DateDeleted == null && x.AccountInfoId == accountInfoId)
                .Select(x => x.AppointmentId)
                .Distinct()
                .ToListAsync();

            unProcessAppointments = unProcessAppointments
                                    .Where(x => !processedAppointmentIds.Contains(x.AppointmentId))
                                    .ToList();

            var appointmentIds = unProcessAppointments.Select(x => x.AppointmentId).Distinct().ToList();

            var appointmentList = await _rethinkService.GetAppointmentListAsync(appointmentIds);

            var count = appointmentList.Where(i => i.startDate >= DateTime.Now.AddDays(unProcessedAppointmentDaysThreshold)).Count();

            return count;
        }

        public async Task<AppointmentModelWithCount> UnprocessedAppointments(UnprocessedAppointmentsRequestModel request)
        {
            var model = _mapper.Map<UnbilledAppointmentsRequestModel>(request);

            var unProcessAppointments = await _linkRepository.Query()
                .Include(c => c.AppointmentClaimProcessingError).AsNoTracking()
                .Where(x => x.ClaimId == 0 && x.DateDeleted == null && x.AccountInfoId == model.AccountInfoId)
                .Select(x => new
                {
                    x.AppointmentId,
                    x.AppointmentClaimProcessingError.ErrorMessage
                }).ToListAsync();

            // Get Appointment with claims
            var processedAppointmentIds = await _linkRepository.Query()
                .Where(x => x.ClaimId != 0 && x.AccountInfoId == model.AccountInfoId)
                .Select(x => x.AppointmentId)
                .Distinct()
                .ToListAsync();

            unProcessAppointments = unProcessAppointments
                                    .Where(x => !processedAppointmentIds.Contains(x.AppointmentId))
                                    .ToList();

            var appointmentIds = unProcessAppointments.Select(x => x.AppointmentId).Distinct().ToList();

            var unprocessedAppointments = new AppointmentModelWithCount();
            var appointmentList = await GetFilteredAppointmentData(appointmentIds, model);

            if (appointmentList.Count == 0)
            {
                unprocessedAppointments.appointmentModels = new List<AppointmentModel>();
                return unprocessedAppointments;
            }

            var count = appointmentList.Count;
            if (model.Take != 0)
                appointmentList = appointmentList.Skip(model.Skip).Take(model.Take).ToList();

            await _appointmentService.SetupRethinkDataForAppointments(appointmentList);
            var mappedAppointmentDetails = await _appointmentService.ToAppointmentItems(model.AccountInfoId, appointmentList, model.MemberId);
            var apptMappedDetails = mappedAppointmentDetails.AsQueryable();

            if (model.SortingModels != null && model.SortingModels.Count > 0)
            {
                apptMappedDetails = apptMappedDetails.OrderBy(model.SortingModels);
            }

            unprocessedAppointments.appointmentModels = apptMappedDetails.ToList();
            unprocessedAppointments.totalCount = count;

            var errorDict = unProcessAppointments
                            .Where(x => !string.IsNullOrEmpty(x.ErrorMessage))
                            .ToDictionary(x => x.AppointmentId, x => x.ErrorMessage);

            foreach (var a in unprocessedAppointments.appointmentModels)
                if (errorDict.TryGetValue(a.Id, out var msg))
                    a.appointmentErrorMessage = msg;

            return unprocessedAppointments;
        }


        private async Task<List<AppointmentRethinkModel>> GetUnbilledData(UnbilledAppointmentsRequestModel model)
        {
            var unBilledAppointments = await _linkRepository
                                        .Query().Include(c => c.Claim)
                                        .Where(x => x.Claim.AccountInfoId == model.AccountInfoId
                                        && x.Claim.StartDate >= model.StartDate && x.Claim.StartDate <= model.EndDate
                                        && (x.Claim.isPrivatePayClaim == null || x.Claim.isPrivatePayClaim == false)) // This is for Self Pay Funder Claim, this type of appointment should not show in this Report.
                                        .ToListAsync();

            if (model.Clients?.Count > 0)
                unBilledAppointments = unBilledAppointments.Where(x => model.Clients.Contains(x.Claim.ChildProfileId)).ToList();

            if (model.PayerOrFunder?.Count > 0)
                unBilledAppointments = unBilledAppointments.Where(x => model.PayerOrFunder.Contains(x.Claim.PrimaryFunderId)).ToList();

            var deletedLinks = unBilledAppointments.Where(x => x.DateDeleted != null).Select(x => x.AppointmentId).Distinct().ToList();
            var activeLinks = unBilledAppointments.Where(x => x.DateDeleted == null).Select(x => x.AppointmentId).Distinct().ToList();

            var unbilledAppointmentIds = deletedLinks.Where(x => !activeLinks.Contains(x)).ToList();

            // Include appointments which are not linked to any claim and also not marked as deleted in link table, as those are also unbilled appointments
            var scheduledAppointmentIdsTask = _unProcessedApointmentScheduleRepository.Query()
                                          .Where(x => x.AccountInfoId == model.AccountInfoId && x.ProcessingStatus == ProcessingState.Unprocessed.ToString())
                                          .Select(x => x.AppointmentId).ToListAsync();

            // Get all completed appointments for the account within the date range to ensure we are only considering completed appointments for unbilled report
            var allCompletedAppointmentsTask = _rethinkService.GetAllCompletedAppointmentsForAnAccountAsync(model.AccountInfoId, model.StartDate ?? DateTime.Now.AddMonths(-3), model.EndDate ?? DateTime.Now, 1);

            await Task.WhenAll(scheduledAppointmentIdsTask, allCompletedAppointmentsTask);

            var scheduledAppointmentIds = scheduledAppointmentIdsTask.Result;
            var allCompletedAppointments = allCompletedAppointmentsTask.Result;

            unbilledAppointmentIds.AddRange(
                scheduledAppointmentIds.Concat(allCompletedAppointments).Where(x => !unbilledAppointmentIds.Contains(x))
            );

            if (unbilledAppointmentIds.Count == 0)
            {
                return [];
            }

            // DOSFilter : false because we have already applied date filter while fetching data from link table based on claim start date and also while fetching completed appointments from rethink, so no need to apply DOS filter again here
            return await GetFilteredAppointmentData(unbilledAppointmentIds, model, applyDOSFilter: false);
        }

        private async Task<List<AppointmentRethinkModel>> GetFilteredAppointmentData(List<int> appointmentIds, UnbilledAppointmentsRequestModel model, bool applyDOSFilter = true)
        {

            var appointmentList = await _rethinkService.GetAppointmentListAsync(appointmentIds);

            #region Filters
            if (model.Clients?.Count > 0)
            {
                appointmentList = appointmentList.Where(i => i.clientId == null || i.clientId == 0 || model.Clients.Contains(i.clientId ?? 0)).ToList();
                if (appointmentList.Count == 0)
                {
                    return [];
                }
            }
            if (model.PayerOrFunder?.Count > 0)
            {
                appointmentList = appointmentList.Where(i => i.funderId == 0 || model.PayerOrFunder.Contains(i.funderId)).ToList();
                if (appointmentList.Count == 0)
                {
                    return [];
                }
            }
            if (model.Staff?.Count > 0)
            {
                appointmentList = appointmentList.Where(i => i.staffId == 0 || model.Staff.Contains(i.staffId)).ToList();
                if (appointmentList.Count == 0)
                {
                    return [];
                }
            }
            if (model.Location?.Count > 0)
            {
                appointmentList = appointmentList.Where(i => i.toLocationId == 0 || model.Location.Contains(i.toLocationId)).ToList();
                if (appointmentList.Count == 0)
                {
                    return [];
                }
            }
            if (model.PlaceOfService?.Count > 0)
            {
                appointmentList = appointmentList.Where(i => i.locationId == null || i.locationId == 0 || model.PlaceOfService.Contains(i.locationId ?? 0)).ToList();
                if (appointmentList.Count == 0)
                {
                    return [];
                }
            }
            if (applyDOSFilter && model.StartDate.HasValue && model.EndDate.HasValue)
            {
                appointmentList = appointmentList.Where(i => i.startDate >= model.StartDate && i.endDate <= model.EndDate).ToList();
                if (appointmentList.Count == 0)
                {
                    return [];
                }
            }
            #endregion

            return appointmentList;
        }

        private static void AddCustomRows(UnbilledAppointmentsRequestModelForExport model, SheetData sheetData)
        {
            var filters = new List<(string Label, string? Value)>
            {
                ("Date Range", $"{model.StartDate?.ToString("MM/dd/yyyy")} - {model.EndDate?.ToString("MM/dd/yyyy")}"),
                ("Member ID", model.MemberId.ToString()),
                ("Account Info ID", model.AccountInfoId.ToString()),
                ("Client IDs", string.Join(", ", model.Clients)),
                ("Staff IDs", string.Join(", ",model.Staff)),
                ("Funder IDs", string.Join(", ",model.PayerOrFunder)),
                ("Service IDs", string.Join(", ",model.PlaceOfService)),
                ("Location IDs", string.Join(", ",model.Location)),
            };

            uint rowIndex = 1;
            foreach (var (label, value) in filters.Where(f => !string.IsNullOrWhiteSpace(f.Value)))
            {
                var row = new Row() { RowIndex = rowIndex++ };
                row.AppendChild(new Cell
                {
                    DataType = CellValues.String,
                    CellValue = new CellValue(label + ""),
                    StyleIndex = (int)ExcelCellDesignStyles.fontWithBoldStyle
                });
                row.AppendChild(new Cell
                {
                    DataType = CellValues.String,
                    CellValue = new CellValue(value)
                });
                sheetData.AppendChild(row);
            }
        }

    }
}
