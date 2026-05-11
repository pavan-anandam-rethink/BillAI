using ClearingHouse.SharedKernel.Domain;

namespace BlobManagement.Domain.ValueObjects;

public class BlobPath : ValueObject
{
    public string ContainerName { get; }
    public string FolderPath { get; }
    public string FileName { get; }
    public string FullPath => string.IsNullOrEmpty(FolderPath) ? FileName : $"{FolderPath}/{FileName}";

    public BlobPath(string containerName, string folderPath, string fileName)
    {
        ContainerName = containerName;
        FolderPath = folderPath;
        FileName = fileName;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ContainerName;
        yield return FolderPath;
        yield return FileName;
    }
}
