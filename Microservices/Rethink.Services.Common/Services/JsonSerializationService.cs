using Newtonsoft.Json;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Services
{
    [ExcludeFromCodeCoverage]
    public class JsonSerializationService : ISerializationService
    {
        public string SerializeObject(object value)
        {
            if (value == null)
                return string.Empty;

            return JsonConvert.SerializeObject(value, new JsonSerializerSettings { Formatting = Formatting.Indented });
        }

        public T DeserializeObject<T>(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return default(T);

            return JsonConvert.DeserializeObject<T>(value);
        }
        public object DeserializeObject(string value, Type type)
        {
            if (String.IsNullOrWhiteSpace(value))
                return null;

            return JsonConvert.DeserializeObject(value, type);
        }
    }
}
