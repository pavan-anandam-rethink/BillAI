using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Billing.FolderStructure.Core.Models
{
    public class TransactionControlNumberModel
    {
        public string? FileType { get; set; }
        public string? NpiNumber { get; set; }
        public string? FederalTaxId { get; set; }
        public int?[]? ControlNumbers { get; set; }
        public string?[]? ClaimIdentifiers { get; set; }
    }
}
