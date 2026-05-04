using System.Collections.Generic;

namespace Rethink.Services.Common.Models
{
    public class PagedResponse<T> : IPagedResponse<T>
    {
        public int Total { get; set; } = 0;
        public List<T> Data { get; set; } = [];
    }
}
