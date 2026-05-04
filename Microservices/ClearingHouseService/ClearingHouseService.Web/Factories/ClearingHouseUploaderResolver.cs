using ClearingHouseService.Web.Interface;

namespace ClearingHouseService.Web.Factories
{
    //this class is worked as a Service Locator / Resolver wrapper for IClearingHouseUploader
    public class ClearingHouseUploaderResolver : IClearingHouseUploaderFactory
    {
        private readonly IServiceProvider _provider;

        public ClearingHouseUploaderResolver(IServiceProvider provider)
        {
            _provider = provider;
        }

        /// <summary>
        /// Resolves the registered IClearingHouseUploader implementation
        /// from the dependency injection container.
        /// This class acts as a thin abstraction over IServiceProvider
        /// to obtain the uploader instance when required.
        /// </summary>
        public IClearingHouseUploader Create()
        {
            return _provider.GetRequiredService<IClearingHouseUploader>();
        }
    }

}
