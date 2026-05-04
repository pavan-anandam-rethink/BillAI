using Billing.FolderStructure.Core.Models;

namespace Billing.FolderStructure.Core.Utils
{
    public class EDI277DetailedReportReader
    {
        public static EDI277CADetailedReport Parse(string ediContent)
        {
            var report = new EDI277CADetailedReport
            {
                ReportDate = DateTime.Today,
                Claims = new List<EDI277DetailedReportSummary>()
            };

            var segments = ediContent.Split('~', StringSplitOptions.RemoveEmptyEntries);
            Dictionary<string, EDI277DetailedReportSummary> claimsByTrn = new();

            string currentTrn = null;
            string tempPatientName = null;
            bool reportTrnSet = false;

            foreach (var segment in segments)
            {
                var parts = segment.Split('*');
                if (parts.Length < 2) continue;

                switch (parts[0])
                {
                    case "BHT":
                        // Capture BHT Number (3rd element, index 2)
                        if (parts.Length > 2)
                        {
                            report.BhtNumber = parts[3];
                        }
                        break;

                    case "NM1":
                        switch (parts[1])
                        {
                            case "AY":
                                report.Sender = parts.Length > 3 ? parts[3] : "UnknownSender";
                                break;
                            case "41":
                                report.Receiver = parts.Length > 3 ? parts[3] : "UnknownReceiver";
                                break;
                            case "QC":
                                // Patient Name comes BEFORE claim TRN, so store temporarily
                                tempPatientName = parts.Length > 3 ? parts[3] : "[Not Found]";
                                // Don't assign here directly because currentTrn may not be set yet
                                break;
                        }
                        break;

                    case "TRN":
                        if (!reportTrnSet)
                        {
                            // First TRN segment after BHT is report-level TRN Reference Number
                            report.TrnReferenceNumber = parts.Length > 2 ? parts[2] : "[Not Found]";
                            reportTrnSet = true;
                        }
                        else
                        {
                            // Claim-level TRN segment
                            currentTrn = parts.Length > 2 ? parts[2] : null;
                            if (currentTrn != null)
                            {
                                if (!claimsByTrn.ContainsKey(currentTrn))
                                {
                                    claimsByTrn[currentTrn] = new EDI277DetailedReportSummary
                                    {
                                        ClaimControlNumber = "[Not Found]",
                                        PatientName = tempPatientName ?? "[Not Found]", // assign patient name stored earlier
                                        StcDescriptions = new List<string>(),
                                        ClaimTrnNumber = currentTrn // explicitly store claim TRN #
                                    };
                                }
                                else if (!string.IsNullOrEmpty(tempPatientName))
                                {
                                    // Update patient name if found later
                                    claimsByTrn[currentTrn].PatientName = tempPatientName;
                                }
                            }
                            tempPatientName = null; // reset after assigning
                        }
                        break;

                    case "STC":
                        if (currentTrn == null) break;

                        if (!claimsByTrn.TryGetValue(currentTrn, out var claimStc))
                        {
                            claimStc = new EDI277DetailedReportSummary
                            {
                                ClaimControlNumber = "[Not Found]",
                                PatientName = "[Not Found]",
                                StcDescriptions = new List<string>()
                            };
                            claimsByTrn[currentTrn] = claimStc;
                        }

                        string stcCode = parts[1];
                        string rejectionReason = parts.Length > 5 ? string.Join(" ", parts.Skip(5)).Trim() : "";

                        claimStc.StcCode = stcCode;
                        claimStc.Status = stcCode.StartsWith("A1") ? "Accepted" : "Rejected";

                        string newDesc = $"Status Code: {stcCode}";
                        if (!claimStc.StcDescriptions.Contains(newDesc))
                        {
                            claimStc.StcDescriptions.Add(newDesc);
                        }

                        claimStc.ActionRequired = rejectionReason;
                        break;

                    case "REF":
                        if (currentTrn != null && claimsByTrn.TryGetValue(currentTrn, out var claimRef))
                        {
                            var qualifier = parts[1];
                            if (qualifier == "F8" || qualifier == "D9" || qualifier == "TJ")
                            {
                                claimRef.ClaimControlNumber = parts.Length > 2 ? parts[2] : "[Not Found]";
                            }
                        }
                        break;
                }
            }

            report.Claims = claimsByTrn.Values
                .Where(c => !string.IsNullOrWhiteSpace(c.StcCode))
                .ToList();

            return report;
        }

    }
}

