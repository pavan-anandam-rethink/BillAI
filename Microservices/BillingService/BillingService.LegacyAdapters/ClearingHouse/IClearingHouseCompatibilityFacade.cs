using BillingService.Domain.Models.Claims;
using Rethink.Services.Common.Models.Claim;
using System.Threading;
using System.Threading.Tasks;

namespace BillingService.LegacyAdapters.ClearingHouse;

/// <summary>
/// Compatibility facade for clearinghouse and EDI workflows.
/// Preserves the full contract of the existing domain service required by the
/// current controllers without alteration to business logic.
/// </summary>
public interface IClearingHouseCompatibilityFacade
{
    /// <summary>Uploads an EDI claim file to blob storage via the domain service.</summary>
    Task<bool> UploadFileAsync(
        ClaimUploadModelWithUserInfo model,
        CancellationToken cancellationToken = default);

    /// <summary>Uploads a clearinghouse EDI response file to blob storage.</summary>
    Task<bool> UploadEdiResponseFileAsync(
        DownloadSftpDataModel fileStreams,
        CancellationToken cancellationToken = default);

    /// <summary>Uploads ERA error file artifacts to blob storage.</summary>
    Task<bool> UploadEraErrorFileAsync(
        ERAUploadModel model,
        CancellationToken cancellationToken = default);

    /// <summary>Uploads Availity EDI response files to the blob backup container.</summary>
    Task<bool> UploadEdIResponseFilesToBlobBackupAsync(
        UploadAvailityFilesModel fileStreams,
        CancellationToken cancellationToken = default);
}
