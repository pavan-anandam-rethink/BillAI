using System.Collections.Generic;

namespace Rethink.Services.Common.Models
{
    public interface IPagedResponse<T>
    {
        int Total { get; set; }
        List<T> Data { get; set; }
    }
}
