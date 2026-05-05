using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace Rethink.Services.Common.Models
{
    [Owned]
    public class RethinkStaffMemberByPermission
    {
        public int total { get; set; }
        public List<RethinkStaffMembersByPermissionResponse> data { get; set; }
    }
    [Owned]
    public class RethinkStaffMembersByPermissionResponse
    {
        public int memberId { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
    }
}
