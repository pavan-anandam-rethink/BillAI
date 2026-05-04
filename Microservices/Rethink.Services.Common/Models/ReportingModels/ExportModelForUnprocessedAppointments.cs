namespace Rethink.Services.Common.Models.ReportingModels
{
    public class ExportModelForUnprocessedAppointments
    {
        public UnbilledAppointmentsRequestModelForExport Filter { get; set; }
        public UnprocessedAppointmentsRequestModel Model { get; set; }
    }
}
