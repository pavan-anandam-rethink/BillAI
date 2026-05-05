using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace Rethink.Services.Common.Models
{
    [Owned]
    public class ReferringProvidersModel
    {
        public int id { get; set; }
        public int accountId { get; set; }
        public ClientAddress address { get; set; }
        public ClientUserName name { get; set; }
        public DateTime? dateOfBirth { get; set; }
        public int timezoneId { get; set; }
        public int? languageId { get; set; }
        public int memberId { get; set; }
        public int? genderId { get; set; }
        public string userType { get; set; }
        public List<ClientUserContact>? contacts { get; set; }
        public List<Identifiers> identifiers { get; set; }
        public List<Options>? options { get; set; }
        public List<Attributes>? attributes { get; set; }
        public bool areInteractionsLogging { get; set; }
        public bool? isLockedOut { get; set; }
        public bool? isApproved { get; set; }
        public MetaData? metaData { get; set; }
    }
}
