using Billing.FolderStructure.Core.Models;

namespace Billing.FolderStructure.Core.Utils
{
    public class EDI999Reader
    {
        public static EDI999Summary Parse(string ediContent, string fileName, string partner)
        {
            var summary = new EDI999Summary
            {
                FileName = fileName,
                Partner = partner,
                TotalTransactionSets = 0,
                Accepted = 0,
                Rejected = 0
            };

            if (string.IsNullOrWhiteSpace(ediContent))
                return summary;

            // Split by ~ and trim spaces
            var segments = ediContent
                .Split('~', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToArray();

            foreach (var segment in segments)
            {
                if (segment.StartsWith("AK9"))
                {
                    var parts = segment.Split('*');
                    if (parts.Length >= 5)
                    {
                        if (int.TryParse(parts[2], out int total))
                            summary.TotalTransactionSets += total;
                        if (int.TryParse(parts[4], out int accepted))
                            summary.Accepted += accepted;
                    }
                }
                else if (segment.StartsWith("IK5"))
                {
                    var parts = segment.Split('*');
                    if (parts.Length >= 2 && parts[1] == "R")
                    {
                        summary.Rejected++;
                    }
                }
            }

            return summary;
        }

    }
}
