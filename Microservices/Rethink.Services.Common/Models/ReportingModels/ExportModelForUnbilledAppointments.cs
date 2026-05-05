using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Models.ReportingModels
{
    public class ExportModelForUnbilledAppointments
    {
        public UnbilledAppointmentsRequestModelForExport Filter { get; set; }
        public UnbilledAppointmentsRequestModel Model { get; set; }
    }
}
