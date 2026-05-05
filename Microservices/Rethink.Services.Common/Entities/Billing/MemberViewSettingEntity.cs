using Rethink.Services.Common.Entities.Base;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rethink.Services.Common.Entities.Billing
{
    public class MemberViewSettingEntity : BasePersistEntity
    {
        public bool Client { get; set; }
        public bool Funder { get; set; }
        public bool RenderingProvider { get; set; }
        public bool PlaceOfService { get; set; }
        public bool DateOfService { get; set; }
        [Column("authColumn")]
        public bool Authorization { get; set; }
        public bool Expected { get; set; }
        public bool Billed { get; set; }
        public bool Payment { get; set; }
        public bool PatientResponsible { get; set; }
        public bool Balance { get; set; }
        public bool BilledDate { get; set; }
        [Column("statusColumn")]
        public bool Status { get; set; }

        public bool AssigneeName { get; set; }

        [Column("validationColumn")]
        public bool Validation { get; set; }
        public bool Adjustment { get; set; }
        public bool Actions { get; set; }
    }
}
