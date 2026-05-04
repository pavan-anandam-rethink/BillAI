using System;

namespace Rethink.Services.Common.Services
{
    public interface ISerializationService
    {
        string SerializeObject(object value);
        T DeserializeObject<T>(string value);
        object DeserializeObject(string value, Type type);
    }
}
