using Rethink.Services.Common.Entities.Base;
using System;
using System.Collections.Generic;

namespace Rethink.Services.Common.Entities.Billing
{
    public class FeatureEntity : BasePersistEntity
    {
        public string FeatureName { get; set; } = null!;

        public DateTime DateCreated { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? DateLastModified { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? DateDeleted { get; set; }
        public int? DeletedBy { get; set; }
        public virtual ICollection<AccountFeatureSettingEntity> AccountFeaturesSettingsEntity { get; set; } = new List<AccountFeatureSettingEntity>();

    }
}
