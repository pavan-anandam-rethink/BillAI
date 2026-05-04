using Newtonsoft.Json;
using System.Net.Http;
using System.Text;

namespace ReportingService.Web.Helpers.HttpClients
{
    internal class JsonContentCustom : StringContent
    {
        public JsonContentCustom(object obj) :
            base(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json")
        { }
    }
}
