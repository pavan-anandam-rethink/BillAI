
using System;

namespace Rethink.Services.Common.Entities.Base
{
    public interface IAuditedEntity<T> : IEntity<T>
    {
        DateTime DateCreated { get; set; }
        DateTime? DateLastModified { get; set; }
        DateTime? DateDeleted { get; set; }
        int CreatedBy { get; set; }
        int? ModifiedBy { get; set; }
        int? DeletedBy { get; set; }
    }

    public interface IAuditedEntity : IEntity, IAuditedEntity<int>
    {

    }

    public interface IOption
    {
        int Option { get; set; }
    }
}
