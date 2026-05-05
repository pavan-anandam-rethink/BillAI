using Microsoft.EntityFrameworkCore;
using System;

namespace Rethink.Services.Common.Models
{
    [Owned]
    public class PropagatingStaffMember
    {
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string middleName { get; set; }
        public int staffCertificationId { get; set; }
        public int staffTitleId { get; set; }
        public string npiNumber { get; set; }
        public string phoneNumber { get; set; }
        public DateTime endDate { get; set; }
        public int id { get; set; }
        public MetaData metaData { get; set; }
    }
}
