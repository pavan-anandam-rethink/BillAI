using Microsoft.EntityFrameworkCore;
using System;

namespace Rethink.Services.Common.Models
{
    [Owned]
    public class AppointmentWorkFlowHistoyModel
    {
        public int id { get; set; }
        public int typeId { get; set; }
        public int statusId { get; set; }
        public string statusDescription { get; set; }
        public int referenceId { get; set; }
        public int createdBy { get; set; }
        public DateTime createdDate { get; set; }
        public DateTime? expirationDate { get; set; }
        public int modifiedBy { get; set; }
        public DateTime dateLastModified { get; set; }
        public int deletedBy { get; set; }
        public DateTime? dateDeleted { get; set; }
    }
}
