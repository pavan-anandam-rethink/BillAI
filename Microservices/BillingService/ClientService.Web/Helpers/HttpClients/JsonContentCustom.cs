using Newtonsoft.Json;
using System.Text;

namespace ClientService.Web.Helpers.HttpClients
{
    internal class JsonContentCustom : StringContent
    {
        public JsonContentCustom(object obj) :
            base(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json")
        { }
    }
}
