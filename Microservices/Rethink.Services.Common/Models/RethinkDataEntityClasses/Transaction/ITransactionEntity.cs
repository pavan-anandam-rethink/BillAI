using Rethink.Services.Common.Entities.Base;
using System.Collections.Generic;

namespace Rethink.Services.Common.Models.RethinkDataEntityClasses.Transaction
{
    public interface ITransactionEntity<T> : IAuditedEntity<T>
    {
        int EntityTypeId { get; }
        int? TypeId { get; set; }
        List<string> PropertiesToExclude { get; }
    }

    public interface ITransactionEntity : IEntity, ITransactionEntity<int>
    {
    }
}