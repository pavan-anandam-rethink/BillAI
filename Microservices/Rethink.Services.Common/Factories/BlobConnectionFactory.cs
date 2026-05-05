using Azure.Storage.Blobs;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Factories
{
    [ExcludeFromCodeCoverage]
    public class BlobConnectionFactory : IBlobConnectionFactory
    {
        public BlobConnectionFactory(string blobConnectionString)
        {
            BlobConnectionString = blobConnectionString;
        }

        public string BlobConnectionString { get; }

        public BlobServiceClient CreateBlobClient()
        {
            return new BlobServiceClient(BlobConnectionString);
        }
    }
}