using System.Text;
using Billing.FolderStructure.Core.Models;

namespace Billing.FolderStructure.Core.Utils
{
    public class EDI277DetailedReportRenderer
    {
        public static string Render(EDI277CADetailedReport report, List<string> claimIds)
        {
            if (report.Claims.Where(x => !claimIds.Contains(x.ClaimTrnNumber)).Count() == 0) return string.Empty;
            var sb = new StringBuilder();
            sb.AppendLine("==========================================================");
            sb.AppendLine("277CA Detailed Error Log");
            sb.AppendLine($"Date: {report.ReportDate:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Sender: {report.Sender}");
            sb.AppendLine($"Receiver: {report.Receiver}");
            sb.AppendLine($"TRN Reference Number (Report Level): {(!string.IsNullOrWhiteSpace(report.TrnReferenceNumber) ? report.TrnReferenceNumber : "[Not Found]")}");
            sb.AppendLine($"BHT Number: {(!string.IsNullOrWhiteSpace(report.BhtNumber) ? report.BhtNumber : "[Not Found]")}");
            sb.AppendLine("----------------------------------------------------------");

            foreach (var c in report.Claims.Where(x => !claimIds.Contains(x.ClaimTrnNumber)))
            {
                sb.AppendLine($"Claim TRN #: {(string.IsNullOrWhiteSpace(c.ClaimTrnNumber) ? "[Not Found]" : c.ClaimTrnNumber)}");
                sb.AppendLine($"Patient Name: {(string.IsNullOrWhiteSpace(c.PatientName) ? "[Not Found]" : c.PatientName)}");
                sb.AppendLine($"Status: {(string.IsNullOrWhiteSpace(c.Status) ? "[Not Found]" : c.Status)}");
                sb.AppendLine($"STC Code: {(string.IsNullOrWhiteSpace(c.StcCode) ? "[Not Found]" : c.StcCode)}");
                sb.AppendLine("STC Description:");
                if (c.StcDescriptions != null && c.StcDescriptions.Count > 0)
                {
                    foreach (var d in c.StcDescriptions)
                        sb.AppendLine($"  - {d}");
                }
                else
                {
                    sb.AppendLine("  - [Not Found]");
                }
                sb.AppendLine($"Action Required: {(string.IsNullOrWhiteSpace(c.ActionRequired) ? "None" : c.ActionRequired)}");
                sb.AppendLine("----------------------------------------------------------");
            }

            sb.AppendLine("==========================================================");
            return sb.ToString();
        }


    }
}
