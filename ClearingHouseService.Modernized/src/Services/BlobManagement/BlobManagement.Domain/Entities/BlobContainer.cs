using ClearingHouse.SharedKernel.Domain;

namespace BlobManagement.Domain.Entities;

public class BlobContainer : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string? RetentionPolicy { get; private set; }
    public bool IsActive { get; private set; }
    public int MaxFileSizeBytes { get; private set; }

    private BlobContainer() { }

    public static BlobContainer Create(string name, string? retentionPolicy = null, int maxFileSizeBytes = 104857600)
    {
        return new BlobContainer
        {
            Name = name,
            RetentionPolicy = retentionPolicy,
            IsActive = true,
            MaxFileSizeBytes = maxFileSizeBytes
        };
    }

    public void Deactivate()
    {
        IsActive = false;
        IncrementVersion();
    }
}
