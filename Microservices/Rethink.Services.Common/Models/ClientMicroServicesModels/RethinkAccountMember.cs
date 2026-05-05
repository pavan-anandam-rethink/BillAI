
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Rethink.Services.Common.Models
{
    [Owned]
    public class RethinkAccountMember
    {
        public int accountId { get; set; }
        public string userName { get; set; }
        public string email { get; set; }
        public string firstName { get; set; }
        public string middleName { get; set; }
        public string lastName { get; set; }
        public string? apiKey { get; set; }
        public List<MemberRoles>? memberRoles { get; set; }
        public int id { get; set; }
        public AccountInfoEntityModel AccountInfo { get; set; }
        public MetaData metaData { get; set; }

    }
    [Owned]
    public class MemberRoles
    {
        public int roleId { get; set; }
        public int memberId { get; set; }
        public int id { get; set; }
        public MetaData? metaData { get; set; }
        public Roles role { get; set; }

    }
    [Owned]
    public class Roles
    {
        public string name { get; set; }
        public string description { get; set; }
        public int id { get; set; }
        public MetaData? metaData { get; set; }
    }
}

