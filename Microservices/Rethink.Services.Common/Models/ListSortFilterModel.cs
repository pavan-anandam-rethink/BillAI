using System.Collections.Generic;

namespace Rethink.Services.Common.Models
{
    public class ListSortFilterModel
    {
        public List<FilterModel> FilterModels { get; set; }
        public List<SortingModel> SortingModels { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
    }
}
