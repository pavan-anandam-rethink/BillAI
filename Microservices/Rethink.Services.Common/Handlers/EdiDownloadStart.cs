using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Handlers
{
    [ExcludeFromCodeCoverage]
    public class EdiDownloadStart
    {
        public string UrlLink { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Title { get; set; }
    }
}