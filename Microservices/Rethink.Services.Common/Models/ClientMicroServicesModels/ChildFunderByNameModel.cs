using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Rethink.Services.Common.Models.ClientMicroServicesModels
{
    [Owned]
    public class ChildFunderByNameModel
    {
        public int total { get; set; }
        public List<FunderDataModel> data { get; set; }
    }
}
