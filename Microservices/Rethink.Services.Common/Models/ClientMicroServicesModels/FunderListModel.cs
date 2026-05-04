
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Rethink.Services.Common.Models.ClientMicroServicesModels
{
    [Owned]
    public class FunderListModel
    {
        public int total { get; set; }
        public List<FunderModel> data { get; set; }
    }
}
