using System.ComponentModel.DataAnnotations.Schema;
using Rethink.Services.Common.Entities.Base;

namespace RethinkAutism.Data.Entities.Members
{
    public class SubscriptionModuleLuEntity : BasePersistEntity
    {
        [Column("ID")]
        public int Id { get; set; }
        [Column("Name")]
        public string Name { get; set; }
    }
}
