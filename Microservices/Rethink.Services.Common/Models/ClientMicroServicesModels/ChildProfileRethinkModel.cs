using Microsoft.EntityFrameworkCore;
using System;

namespace Rethink.Services.Common.Models
{
    [Owned]
    public class ChildProfileRethinkModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public DateTime? DateDeleted { get; set; }
    }
}
