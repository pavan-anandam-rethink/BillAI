using Microsoft.EntityFrameworkCore;

namespace Rethink.Services.Common.Models.ClientMicroServicesModels
{
    [Owned]
    public class ClientDetailsModel
    {
        public bool showClinical { get; set; }
        public bool showScheduling { get; set; }
        public bool showBilling { get; set; }
        public int serviceIntensityTypeId { get; set; }
        public int id { get; set; }
        public MetaData? metaData { get; set; }
    }
}
