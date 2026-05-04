using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillingService.Domain.Models
{
    public class AttachmentViewModel
    {
        public int Id { get; set; }
        public string Filename { get; set; }
        public DateTime DateCreated { get; set; }
        //tblMember is in another (bh) database
        [NotMapped]
        public string CreatedBy { get; set; }
    }
}