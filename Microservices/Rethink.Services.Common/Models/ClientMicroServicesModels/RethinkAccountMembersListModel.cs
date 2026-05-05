using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Rethink.Services.Common.Models
{
    [Owned]
    public class RethinkAccountMembersListModel
    {
        public int total { get; set; }
        public List<RethinkAccountMember> data { get; set; }
    }
}
