using Newtonsoft.Json;
using System;

namespace RethinkAutism.Contracts.DataObjects.Curriculum
{
    public class SessionNoteComment
    {
        public int Id { get; set; }
        public int AppointmentId { get; set; }
        public string Comment { get; set; }
        public int ReviewedById { get; set; }
        public string ReviewedByName { get; set; }
        public DateTime ReviewedOn { get; set; }
        public int CreatedById { get; set; }
        public string CreatedByName { get; set; }
        public DateTime DateCreated { get; set; }

        public void WriteJson(JsonTextWriter writer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("Id");
            writer.WriteValue(Id);
            writer.WritePropertyName("AppointmentId");
            writer.WriteValue(AppointmentId);
            writer.WritePropertyName("Comment");
            writer.WriteValue(Comment);
            writer.WritePropertyName("ReviewedById");
            writer.WriteValue(ReviewedById); ;
            writer.WritePropertyName("ReviewedByName");
            writer.WriteValue(ReviewedByName); ;
            writer.WritePropertyName("ReviewedOn");
            writer.WriteValue(ReviewedOn); ;
            writer.WritePropertyName("CreatedById");
            writer.WriteValue(CreatedById); ;
            writer.WritePropertyName("CreatedByName");
            writer.WriteValue(CreatedByName); ;
            writer.WritePropertyName("DateCreated");
            writer.WriteValue(DateCreated);

            writer.WriteEndObject();
        }
    }
}
