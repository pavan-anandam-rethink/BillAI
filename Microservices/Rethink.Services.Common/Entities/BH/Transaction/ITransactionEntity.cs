using System.Collections.Generic;
using Rethink.Services.Common.Entities.Base;

namespace Rethink.Services.Common
{
    public interface ITransactionEntity<T>: IAuditedEntity<T>
    {
        int EntityTypeId { get; }
        int? TypeId { get; set; }
        List<string> PropertiesToExclude { get; }
    }
    
    public interface ITransactionEntity : IEntity, ITransactionEntity<int>
    {
    }
}