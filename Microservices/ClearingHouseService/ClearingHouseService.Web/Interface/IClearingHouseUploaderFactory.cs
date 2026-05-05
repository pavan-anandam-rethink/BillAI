namespace ClearingHouseService.Web.Interface
{
    // This interface defines a factory contract for creating instances of IClearingHouseUploader.
    public interface IClearingHouseUploaderFactory
    {
        IClearingHouseUploader Create();
    }
}
