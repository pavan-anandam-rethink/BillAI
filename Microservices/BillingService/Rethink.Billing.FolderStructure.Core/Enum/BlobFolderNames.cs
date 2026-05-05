using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Billing.FolderStructure.Core.Enum
{
    public enum BlobFolderNames
    {
        Incoming,
        Archive,
        ERA,
        EOB,
        Processing,
        Failed,
        Logs,
        ProcessingLogs,
        ErrorLogs,
        Outgoing,
        Processed,
        Accepted,
        AcceptedWithErrors,
        Rejected,
        Errors,
        Reports,
        Daily,
        Detailed,
        Duplicate
    }
}
