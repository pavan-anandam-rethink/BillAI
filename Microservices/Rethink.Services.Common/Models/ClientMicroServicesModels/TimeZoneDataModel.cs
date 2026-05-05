using System.Text.Json.Serialization;

namespace Rethink.Services.Common.Models.ClientMicroServicesModels;

public class TimeZoneDataModel
{
    [JsonPropertyName("id")]
    public int Id { set; get; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
}