namespace Billing.FolderStructure.Core
{
    public class BillingStorageConfig
    {
        public string ContainerName { get; set; }
        public Dictionary<string, string[]> Sources { get; set; }  // Availity / Stedi
        public string[] Accounts { get; set; }
        public Dictionary<string, string[]> FolderStructure { get; set; }
    }

}
