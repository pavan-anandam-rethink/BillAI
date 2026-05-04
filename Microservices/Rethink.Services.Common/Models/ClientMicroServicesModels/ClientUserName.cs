
using Microsoft.EntityFrameworkCore;

namespace Rethink.Services.Common.Models
{
    [Owned]
    public class ClientUserName
    {
        public string firstName { get; set; }
        public string? middleName { get; set; }
        public string lastName { get; set; }
        public string prefix { get; set; }
        public string suffix { get; set; }
    }
}
