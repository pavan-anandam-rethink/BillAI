namespace BillingService.Domain
{
    public class ValidationErrorMessages
    {
        public static string NotFound(string name)
        {
            return string.Format("{0} not found", name);
        }
    }
}
