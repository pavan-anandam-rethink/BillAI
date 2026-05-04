using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Billing.FolderStructure.Core.Models
{
    public class BillingFolderStructureModel
    {
        public byte[] Data { get; set; }
        public string FileName { get; set; }
        public string? Message { get; set; }
        public int? AccountInfoId { get; set; }
        public int? ClearingHouseId { get; set; }
        public int? TransactionNumber { get; set; }
        public string? ClearingHouseTitle { get; set; }
        public int? PaymentId { get; set; }
    }
}
