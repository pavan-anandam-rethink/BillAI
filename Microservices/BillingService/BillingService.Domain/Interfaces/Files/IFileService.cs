namespace BillingService.Domain.Interfaces.Files
{
    public interface IFileService
    {
        string PrepareFolderForEncounterAttachmentFile(int accountId, string fileName, string folderName, bool isHealthCare, int? referenceId = null, string? claimidentifier = null);
        string PrepareFolderForERAErrorFile(int accountId);
    }
}
