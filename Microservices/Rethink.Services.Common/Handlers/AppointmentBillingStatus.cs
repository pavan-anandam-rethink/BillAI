using Rethink.Services.Common.Enums.BH;
using System;
using System.Diagnostics.CodeAnalysis;
namespace Rethink.Services.Common.Handlers
{
    [ExcludeFromCodeCoverage]
    public class AppointmentBillingStatus
    {
        public int AppointmentId { get; set; }
        public RethinkBillingStatus BillingStatus { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}
