using Rethink.Services.Common.Entities.Base;
using System;

namespace Rethink.Services.Common.Entities.Billing
{
    public class AccountFeatureSettingEntity : BasePersistEntity
    {
        
        // Foreign Key FeatureEntity
       
        public int FeatureId { get; set; }

        public int AccountId { get; set; }

        public bool Status { get; set; }


        public DateTime DateCreated { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? DateLastModified { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? DateDeleted { get; set; }
        public int? DeletedBy { get; set; }

        public virtual FeatureEntity FeatureEntity { get; set; } 
    }
}
