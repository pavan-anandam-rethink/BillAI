using Azure.Storage.Blobs;

namespace Rethink.Services.Common.Factories
{
    public interface IBlobConnectionFactory
    {
        BlobServiceClient CreateBlobClient();
    }
}