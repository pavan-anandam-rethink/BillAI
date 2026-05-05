
using Microsoft.EntityFrameworkCore;
using System;

namespace Rethink.Services.Common.Models
{
    [Owned]
    public class ClientContactsModel
    {
        public MetaData metaData { get; set; }
        public int id { get; set; }
        public int userId { get; set; }
        public string userType { get; set; }
        public ClientUserName name { get; set; }
        public ClientAddress Address { get; set; }
        public string email { get; set; }
        public string phoneNumber { get; set; }
        public string relationToClient { get; set; }
        public int timezoneId { get; set; }
        public bool isPrimaryContact { get; set; }
        public int genderId { get; set; }
        public int? maritalStatusId { get; set; }
        public DateTime dateOfBirth { get; set; }
    }
}
