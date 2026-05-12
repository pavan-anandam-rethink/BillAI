namespace ClearingHouse.SharedKernel.Domain.ValueObjects;

/// <summary>
/// Value object representing a reference to a file stored in blob storage.
/// </summary>
public sealed record FileReference
{
    /// <summary>
    /// Gets the blob storage container name.
    /// </summary>
    public string ContainerName { get; }

    /// <summary>
    /// Gets the blob path within the container.
    /// </summary>
    public string BlobPath { get; }

    /// <summary>
    /// Gets the original file name.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Gets the file size in bytes.
    /// </summary>
    public long FileSize { get; }

    /// <summary>
    /// Gets the content hash (SHA-256) for integrity verification.
    /// </summary>
    public string ContentHash { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileReference"/> record.
    /// </summary>
    public FileReference(string containerName, string blobPath, string fileName, long fileSize, string contentHash)
    {
        if (string.IsNullOrWhiteSpace(containerName))
            throw new ArgumentException("Container name is required.", nameof(containerName));
        if (string.IsNullOrWhiteSpace(blobPath))
            throw new ArgumentException("Blob path is required.", nameof(blobPath));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));
        if (fileSize < 0)
            throw new ArgumentOutOfRangeException(nameof(fileSize), "File size cannot be negative.");
        if (string.IsNullOrWhiteSpace(contentHash))
            throw new ArgumentException("Content hash is required.", nameof(contentHash));

        ContainerName = containerName;
        BlobPath = blobPath;
        FileName = fileName;
        FileSize = fileSize;
        ContentHash = contentHash;
    }

    /// <summary>
    /// Gets the full blob URI path.
    /// </summary>
    public string FullPath => $"{ContainerName}/{BlobPath}";
}
