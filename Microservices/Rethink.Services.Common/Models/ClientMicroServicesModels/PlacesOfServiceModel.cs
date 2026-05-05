using Microsoft.EntityFrameworkCore;
using Rethink.Services.Common.Models;
using System.Collections.Generic;

namespace BillingService.Domain.Models.ClientMicroServicesModels
{
    [Owned]
    public class PlacesOfServiceModel
    {
        public PlaceOfServicesList placesOfService { get; set; }
    }

    public class PlaceOfServicesList
    {
        public int total { get; set; }
        public List<placeOfService> data { get; set; }
    }
    public class placeOfService
    {
        public int accountId { get; set; }
        public string description { get; set; }
        public string code { get; set; }
        public bool isActive { get; set; }
        public int id { get; set; }
        public MetaData? metaData { get; set; }
    }

}
