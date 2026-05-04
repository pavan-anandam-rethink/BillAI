using Rethink.Services.Common.Models;
using System.Collections.Generic;

namespace BillingService.Domain.Models.BillingSettings
{
    public class FeatureStatusDto
    {
        public int FeatureId { get; set; }
        public string FeatureName { get; set; } = null!;
        public bool IsEnabled { get; set; }

    }

}
