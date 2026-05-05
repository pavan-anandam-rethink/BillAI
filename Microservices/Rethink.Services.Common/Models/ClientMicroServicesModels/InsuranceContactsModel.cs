using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace Rethink.Services.Common.Models
{
    [Owned]
    public class InsuranceContactsModel
    {
        public int total { get; set; }
        public List<InsuranceContacts> data { get; set; }
    }
    [Owned]
    public class InsuranceContacts
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int GuarantorId { get; set; }
        public bool IsGuarantor { get; set; }
        public string UserType { get; set; }
        public ClientUserName Name { get; set; }
        public ClientAddress Address { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string RelationToClient { get; set; }
        public int RelationshipToInsured { get; set; }
        public int TimezoneId { get; set; }
        public bool IsPrimaryContact { get; set; }
        public int GenderId { get; set; }
        public int? MaritalStatusId { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public InsuranceContactsTypeModel InsuranceContactsType { get; set; }
        public MetaData MetaData { get; set; }
    }
}
