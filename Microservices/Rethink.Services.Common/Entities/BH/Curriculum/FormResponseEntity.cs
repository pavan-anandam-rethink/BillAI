using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.BH.Member;
using RethinkAutism.Data.Entities.Members;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace RethinkAutism.Data.Entities.Curriculum
{
    public class FormResponseEntity : BasePersistEntity, IAuditedEntity
    {

        [Column("hcFormId")]
        public int FormId { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("childProfileId")]
        public int? ChildProfileId { get; set; }
        [Column("Memberid")]
        public int MemberId { get; set; }
        [Column("hcServiceId")]
        public int? ServiceId { get; set; }
        [Column("ReferenceId")]
        public int? ReferenceId { get; set; }
        [Column("isAutoSave")]
        public bool? IsAutoSave { get; set; }
        [Column("status")]
        public int? Status { get; set; }
        [Column("RemoteSignatureRequestId")]
        public int? RemoteSignatureRequestId { get; set; }




        [Column("CreatedBy")]
        public int CreatedBy { get; set; }
        [Column("ModifiedBy")]
        public int? ModifiedBy { get; set; }
        [Column("DeletedBy")]
        public int? DeletedBy { get; set; }
        [Column("DateCreated")]
        public DateTime DateCreated { get; set; }
        [Column("DateLastModified")]
        public DateTime? DateLastModified { get; set; }
        [Column("DateDeleted")]
        public DateTime? DateDeleted { get; set; }


        public virtual FormEntity Form { get; set; }
        public virtual MemberEntity Member { get; set; }
        public RemoteSignatureRequestEntity RemoteSignatureRequest { get; set; }
    }
}
