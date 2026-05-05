using EdiFabric.Core.Model.Edi.X12;
using System;
using System.Linq;
using System.Reflection;
//using EdiFabric.Templates.X12004010;

namespace BillingService.Domain.Services.Billing.EDI
{
    public static class SegmentBuilders
    {
        /// <summary>
        /// Build ISA.
        /// </summary>
        public static ISA BuildIsa(string controlNumber,
            string securityInfo,
            string senderId,
            string receiverId,
            string testIndicator, // "T" = test, "P" = production
            string ackRequested = "1")
        {
            return new ISA
            {
                //  Authorization Information Qualifier
                AuthorizationInformationQualifier_1 = "00",
                //  Authorization Information
                AuthorizationInformation_2 = "".PadRight(10),
                //  Security Information Qualifier
                SecurityInformationQualifier_3 = "00",
                //  Security Information
                SecurityInformation_4 = securityInfo.PadRight(10),
                //  Interchange ID Qualifier
                SenderIDQualifier_5 = "ZZ",
                //  Interchange Sender
                InterchangeSenderID_6 = senderId.PadRight(15),
                //  Interchange ID Qualifier
                ReceiverIDQualifier_7 = "01",
                //  Interchange Receiver
                InterchangeReceiverID_8 = receiverId.PadRight(15),
                //  Date
                InterchangeDate_9 = DateTime.Now.Date.ToString("yyMMdd"),
                //  Time
                InterchangeTime_10 = DateTime.Now.TimeOfDay.ToString("hhmm"),
                //  Standard identifier
                InterchangeControlStandardsIdentifier_11 = "^",
                //  Interchange Version ID
                //  This is the ISA version and not the transaction sets versions
                InterchangeControlVersionNumber_12 = "00501",
                //  Interchange Control Number
                InterchangeControlNumber_13 = controlNumber.PadLeft(9, '0'),
                //  Acknowledgment Requested (0 or 1)
                AcknowledgementRequested_14 = ackRequested,
                //  Test Indicator -- ISA-15: This indicates whether data enclosed by this interchange envelope is test ‘T’,
                //  production ‘P’, or information ‘I’. 
                UsageIndicator_15 = testIndicator,
            };
        }

        /// <summary>
        /// Build GS.
        /// </summary>
        public static GS BuildGs(string controlNumber,
                                 string customerId,
                                 string receiverId,
                                 string version)
        {
            return new GS
            {
                //  Functional ID Code
                CodeIdentifyingInformationType_1 = "HC",
                //  Application Senders Code
                SenderIDCode_2 = customerId,
                //  Application Receivers Code
                ReceiverIDCode_3 = receiverId,
                //  Date
                Date_4 = DateTime.Now.Date.ToString("yyyyMMdd"),
                //  Time
                Time_5 = DateTime.Now.TimeOfDay.ToString("hhmm"),
                //  Group Control Number
                //  Must be unique to both partners for this interchange
                GroupControlNumber_6 = controlNumber.PadLeft(9, '0'),
                //  Responsible Agency Code
                TransactionTypeCode_7 = "X",
                //  Version/Release/Industry id code
                VersionAndRelease_8 = version
            };
        }


        public static ISA BuildIsaForStedi(string controlNumber,
                                            string senderId,          // MUST match Stedi SFTP sender ID
                                            string receiverId,        // STEDITEST (test) or production ID
                                            string testIndicator)     // "T" or "P"
        {
            return new ISA
            {
                AuthorizationInformationQualifier_1 = "00",
                AuthorizationInformation_2 = "".PadRight(10),
                SecurityInformationQualifier_3 = "00",
                SecurityInformation_4 = "".PadRight(10),

                SenderIDQualifier_5 = "ZZ",
                InterchangeSenderID_6 = senderId.PadRight(15),

                // 🔥 IMPORTANT: Stedi uses ZZ, not 01
                ReceiverIDQualifier_7 = "ZZ",
                InterchangeReceiverID_8 = receiverId.PadRight(15),

                InterchangeDate_9 = DateTime.UtcNow.ToString("yyMMdd"),
                InterchangeTime_10 = DateTime.UtcNow.ToString("HHmm"),

                InterchangeControlStandardsIdentifier_11 = "^",
                InterchangeControlVersionNumber_12 = "00501",
                InterchangeControlNumber_13 = controlNumber.PadLeft(9, '0'),

                AcknowledgementRequested_14 = "0",
                UsageIndicator_15 = testIndicator
            };
        }



    }


    public static class SegmentBuilders270
    {
        private static string PadRightFixed(string input, int length) =>
            (input ?? string.Empty).PadRight(length).Substring(0, length);

        private static string PadLeftFixed(string input, int length) =>
            (input ?? string.Empty).PadLeft(length).Substring(0, Math.Min(input?.Length ?? 0, length));

        /// <summary>
        /// Build ISA. NOTE: follows X12 fixed-width rules.
        /// </summary>
        public static ISA BuildIsa(
            string controlNumber,
            string securityInfo,
            string senderId,
            string receiverId,
            string testIndicator,    // "T" or "P"
            string senderQualifier = "ZZ",
            string receiverQualifier = "ZZ",
            string ackRequested = "1",
            char repetitionSeparator = '^',   // ISA11 (commonly '^')
            char componentElementSeparator = ':' // ISA16 (commonly ':')
        )
        {
            if (string.IsNullOrWhiteSpace(controlNumber))
                throw new ArgumentException("controlNumber is required", nameof(controlNumber));

            // Normalize control number to numeric digits only and pad to 9
            var digitsOnly = new string((controlNumber ?? "").Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(digitsOnly))
                throw new ArgumentException("controlNumber must contain digits", nameof(controlNumber));
            var isa13 = digitsOnly.PadLeft(9, '0');

            // Use UTC and 24-hour time for clarity
            var now = DateTime.UtcNow;

            var isa = new ISA
            {
                // ISA01 - Authorization Information Qualifier (00 when none)
                AuthorizationInformationQualifier_1 = "00",

                // ISA02 - Authorization Information (10 chars fixed) -> spaces when unused
                AuthorizationInformation_2 = new string(' ', 10),

                // ISA03 - Security Information Qualifier (00 when none)
                SecurityInformationQualifier_3 = "00",

                // ISA04 - Security Information (10 chars fixed) -> spaces when unused or pad provided securityInfo
                SecurityInformation_4 = PadRightFixed(securityInfo, 10),

                // ISA05 - Sender ID Qualifier (2 chars)
                SenderIDQualifier_5 = (senderQualifier ?? "ZZ").PadRight(2).Substring(0, 2),

                // ISA06 - Interchange Sender ID (15 chars fixed) - right padded
                InterchangeSenderID_6 = PadRightFixed(receiverId, 15),

                // ISA07 - Receiver ID Qualifier (2 chars)
                ReceiverIDQualifier_7 = (receiverQualifier ?? "ZZ").PadRight(2).Substring(0, 2),

                // ISA08 - Interchange Receiver ID (15 chars fixed) - right padded
                InterchangeReceiverID_8 = PadRightFixed(senderId, 15),

                // ISA09 - Interchange Date (YYMMDD)
                InterchangeDate_9 = now.ToString("yyMMdd"),

                // ISA10 - Interchange Time (HHMM 24-hour)
                InterchangeTime_10 = now.ToString("HHmm"),

                // ISA11 - Repetition Separator (single char, historically '^')
                InterchangeControlStandardsIdentifier_11 = repetitionSeparator.ToString(),

                // ISA12 - Interchange Control Version Number
                InterchangeControlVersionNumber_12 = "00501",

                // ISA13 - Interchange Control Number (9-digit)
                InterchangeControlNumber_13 = isa13,

                // ISA14 - Acknowledgment Requested
                AcknowledgementRequested_14 = ackRequested ?? "0",

                // ISA15 - Usage Indicator (T or P)
                UsageIndicator_15 = testIndicator == "T" ? "T" : "P"
            };

            // Set ISA16 (component element separator) if the ISA class exposes a property.
            // Common property names differ between libraries; try a few possibilities.
            var isaType = isa.GetType();
            var propNames = new[]
                             {
                                "ComponentElementSeparator_16",
                                "InterchangeControlElementSeparator_16",
                                "ComponentElementSeparator",
                                "ElementSeparator"
                            };
            bool setProp = false;
            foreach (var name in propNames)
            {
                var p = isaType.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (p != null && p.CanWrite)
                {
                    p.SetValue(isa, componentElementSeparator.ToString());
                    setProp = true;
                    break;
                }
            }

            // If ISA class doesn't expose ISA16, ensure the writer settings supply it (do that in ToEdi).
            if (!setProp)
            {
                // No-op here; ToEdi should set separators.ComponentDataElement = componentElementSeparator
            }

            return isa;
        }

        public static GS BuildGs(
            string groupControlNumber,
            string senderCode,
            string receiverCode,
            string version)
        {
            if (string.IsNullOrWhiteSpace(groupControlNumber))
                throw new ArgumentException("groupControlNumber is required", nameof(groupControlNumber));

            var gs = new GS
            {
                CodeIdentifyingInformationType_1 = "HS",
                SenderIDCode_2 = senderCode,
                ReceiverIDCode_3 = receiverCode,
                Date_4 = DateTime.UtcNow.ToString("yyyyMMdd"),
                Time_5 = DateTime.UtcNow.ToString("HHmm"),
                // GS06 commonly not fixed-width; use raw numeric value but ensure GE02 uses the same
                GroupControlNumber_6 = groupControlNumber,
                TransactionTypeCode_7 = "X",
                VersionAndRelease_8 = version
            };

            return gs;
        }
    }

}
