using System.Collections.Generic;

namespace Rethink.Services.Common.Models.ReportingModels
{
    public class AccountsReceivablesResponseModel
    {
        public List<AccountsReceivablesResponse> AccountsReceivables { get; set; }
        public int totalCount { get; set; }
    }
}
