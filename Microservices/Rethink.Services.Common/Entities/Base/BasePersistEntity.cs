using System.ComponentModel.DataAnnotations;

namespace Rethink.Services.Common.Entities.Base
{
    public class BasePersistEntity : IEntity
    {
        [Key]
        public virtual int Id { get; set; }
    }
}
