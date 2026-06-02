using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models.Claims;
using Rethink.Services.Common.Models.Claim;
using System.Threading;
using System.Threading.Tasks;

namespace BillingService.LegacyAdapters.ClearingHouse;

/// <summary>
/// Wraps <see cref="ICHService"/> for injection into CQRS handlers and future
/// Application-layer extraction. All calls delegate unchanged to the legacy domain
/// service to ensure zero functional regression.
///
/// Blob-first invariant: no blob path or URL is returned from or stored by this
/// facade. Content retrieval must use blob name + container derived from
/// correlation/aggregate context.
/// </summary>
public sealed class ClearingHouseCompatibilityFacade(ICHService clearingHouseService)
    : IClearingHouseCompatibilityFacade
{
    public Task<bool> UploadFileAsync(
        ClaimUploadModelWithUserInfo model,
        CancellationToken cancellationToken = default)
    {
        return clearingHouseService.UploadFileAsync(model);
    }

    public Task<bool> UploadEdiResponseFileAsync(
        DownloadSftpDataModel fileStreams,
        CancellationToken cancellationToken = default)
    {
        return clearingHouseService.UploadEDIResponseFile(fileStreams);
    }

    public Task<bool> UploadEraErrorFileAsync(
        ERAUploadModel model,
        CancellationToken cancellationToken = default)
    {
        return clearingHouseService.UploadERAErrorFileAsync(model);
    }

    public Task<bool> UploadEdIResponseFilesToBlobBackupAsync(
        UploadAvailityFilesModel fileStreams,
        CancellationToken cancellationToken = default)
    {
        return clearingHouseService.UploadEDIResponseFilesToBlobBackup(fileStreams);
    }
}
