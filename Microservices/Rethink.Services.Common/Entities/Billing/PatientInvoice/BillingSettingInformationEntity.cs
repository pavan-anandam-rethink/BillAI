using Microsoft.EntityFrameworkCore;
using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Models;
using System;

namespace Rethink.Services.Common.Entities.Billing.PatientInvoice
{

    public class BillingSettingInformationEntity : BasePersistEntity
    {
        public int Id { get; set; }

        public int PayToAddressOverrideOption { get; set; }

        public string? CompanyName { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Zip { get; set; }
        public string? ZipExtension { get; set; }
        public int AccountId { get; set; }

        public string? DunningMessage { get; set; }
        public string? GlobalMessage { get; set; }

        public DateTime DateCreated { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? DateLastModified { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? DateDeleted { get; set; }
        public int? DeletedBy { get; set; }
    }
}