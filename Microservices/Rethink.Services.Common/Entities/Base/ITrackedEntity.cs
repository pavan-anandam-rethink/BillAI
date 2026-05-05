using System;

namespace Rethink.Services.Common.Entities.Base
{
    public interface ITrackedEntity<T> : IEntity<T>
    {
        DateTime DateCreated { get; set; }
        DateTime DateLastModified { get; set; }
        DateTime? DateDeleted { get; set; }
    }

    public interface ITrackedEntity : IEntity, ITrackedEntity<int>
    {
    }
}
