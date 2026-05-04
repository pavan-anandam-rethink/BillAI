using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Models.ReportingModels
{
    public class UnbilledAppointmentsRequestModelForExport
    {
        public List<string> PayerOrFunder { get; set; }
        public List<string> Clients { get; set; }
        public List<string> Staff { get; set; }
        public List<string> Location { get; set; }
        public List<string> PlaceOfService { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int AccountInfoId { get; set; }
        public int MemberId { get; set; }
    }
}
