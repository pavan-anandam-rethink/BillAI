using System;
using System.Collections.Generic;
using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.BH.Member;

namespace Rethink.Services.Common.Entities.BH.Company
{
    public class CountryEntity : BasePersistEntity, ITrackedEntity
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public int? Pos { get; set; }
        public int? UTCOffSet { get; set; }
        public int? UTCDSTOffSet { get; set; }

        public DateTime DateCreated { get; set; }
        public DateTime DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        public virtual ICollection<ChildProfileEntity> ChildProfiles { get; set; }
    }
}