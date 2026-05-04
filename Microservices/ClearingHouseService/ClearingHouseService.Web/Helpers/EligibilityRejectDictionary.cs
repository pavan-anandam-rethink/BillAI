namespace ClearingHouseService.Web.Helpers
{
    public static class EligibilityRejectDictionary
    {
        public static readonly Dictionary<string, string> RejectCodeMessages = new Dictionary<string, string>
        {
            // Reason codes with human-readable messages 
            { "42", "Unable to respond at current time" },
            { "43", "Invalid or Missing Provider Identification" },
            { "72", "Claim / eligibility not found" },
            { "73", "Invalid Subscriber ID" },
            {"75", "Subscriber not found,Validate patient information" }
        };
    }
}
