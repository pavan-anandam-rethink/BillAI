using System;
using System.ComponentModel.DataAnnotations.Schema;
using Rethink.Services.Common.Entities.Base;

namespace Rethink.Services.Common.Entities.BH.Member
{
    public class PersonEntity : BasePersistEntity, IAuditedEntity
    {
        [Column("EncryptedFirstName")]
        public string EncryptedFirstName { get; set; }
        [Column("EncryptedLastName")]
        public string EncryptedLastName { get; set; }
        [Column("Phone")]
        public string Phone { get; set; }
        [Column("Fax")]
        public string Fax { get; set; }
        [Column("Email")]
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        [Column("Phone2")]
        public string Phone2 { get; set; }
        [Column("Phone3")]
        public string Phone3 { get; set; }
        [Column("SMSPhone")]
        public string SMSPhone { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
    }
}