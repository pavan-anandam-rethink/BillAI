using System;
using System.ComponentModel.DataAnnotations.Schema;
using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.BH.Member;

namespace RethinkAutism.Data.Entities.Curriculum
{
    public class PayOverEntity : BasePersistEntity, IAuditedEntity
    {
        public int AccountInfoId { get; set; }
        public int StartWorkweekDayTypeId { get; set; }
        public int EndWorkweekDayTypeId { get; set; }
        [Column("hcPayOverOptionId")]
        public int? HcPayOverOptionId { get; set; }
        public bool? IsActiveOverTime1 { get; set; }
        public bool? IsActiveOverTime2 { get; set; }
        public decimal? OverTimeRate1 { get; set; }
        public decimal? OverTimeRate2 { get; set; }
        public int? StartOverTimeByDay1 { get; set; }
        public int? StartOverTimeByDay2 { get; set; }
        public int? StartOverTimeByWeek1 { get; set; }
        public int? StartOverTimeByWeek2 { get; set; }
        public int? StartOverTimeBy7Day1 { get; set; }
        public int? StartOverTimeBy7Day2 { get; set; }

        [NotMapped]
        public int? OverTimeRateDigit1 { get; set; }
        [NotMapped]
        public int? OverTimeRateDecimal1 { get; set; }
        [NotMapped]
        public int? OverTimeRateDigit2 { get; set; }
        [NotMapped]
        public int? OverTimeRateDecimal2 { get; set; }

        public int CreatedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        public int? DeletedBy { get; set; }

        public int? WorkweekId { get; set; }
        public DateTime? WorkweekLastChange { get; set; }

        public virtual AccountInfoEntity AccountInfo { get; set; }

    }
}
