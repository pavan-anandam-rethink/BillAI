using Microsoft.EntityFrameworkCore.ChangeTracking;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rethink.Services.Common.Models.RethinkDataEntityClasses.Transaction
{
    public class TransactionEntry
    {
        public TransactionEntry(EntityEntry entry)
        {
            Entry = entry;
        }
        public int Id { get; set; }
        public EntityEntry Entry { get; }
        public string TableName { get; set; }
        public int ReferenceId { get; set; }
        public int ReferenceTypeId { get; set; }
        public int TransactionBy { get; set; }
        public int? TypeId { get; set; }
        public Dictionary<string, object> OldValues { get; } = new Dictionary<string, object>();
        public Dictionary<string, object> NewValues { get; } = new Dictionary<string, object>();
        public string Action { get; set; }

        public List<PropertyEntry> TemporaryProperties { get; } = new List<PropertyEntry>();

        public bool HasTemporaryProperties => TemporaryProperties.Any();

        public TransactionEntity ToTransaction()
        {
            var easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var easternTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, easternZone);

            var transaction = new TransactionEntity();
            transaction.TableName = TableName;
            transaction.ReferenceId = ReferenceId;
            transaction.ReferenceTypeId = ReferenceTypeId;
            transaction.TransactionBy = TransactionBy;
            transaction.TransactionOn = easternTime;
            transaction.TypeId = TypeId != null ? TypeId.Value : 1; // default to manual
            transaction.OldValues = OldValues.Count == 0 ? null : JsonConvert.SerializeObject(OldValues);
            transaction.NewValues = NewValues.Count == 0 ? null : JsonConvert.SerializeObject(NewValues);
            transaction.Action = Action;
            transaction.DateCreated = easternTime;
            transaction.CreatedBy = TransactionBy;
            return transaction;
        }
    }
}