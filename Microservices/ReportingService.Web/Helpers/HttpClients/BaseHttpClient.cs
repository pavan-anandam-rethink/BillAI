using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace ReportingService.Web.Helpers.HttpClients
{
    public class BaseHttpClient : IBaseHttpClient
    {
        private readonly HttpClient _httpClient;

        public BaseHttpClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public virtual async Task<string> PostAsync(string baseAddress, string endpoint, object body)
        {
            _httpClient.BaseAddress = new Uri(baseAddress);
            var response = await _httpClient.PostAsync(endpoint, new JsonContentCustom(body));

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var resStr = await response.Content.ReadAsStringAsync();

                return resStr;
            }

            return null;
        }
    }
}