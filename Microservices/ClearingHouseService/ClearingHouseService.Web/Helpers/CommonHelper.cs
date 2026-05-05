using Billing.FolderStructure.Core.Enum;
using BillingService.Domain.Models.Claims;
using ClearingHouseService.Web.Interface;
using ClearingHouseService.Web.Service;
using Newtonsoft.Json;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Models.EligibilityRequest;
using Rethink.Services.Domain.Interfaces;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace ClearingHouseService.Web.Helpers
{
    public class CommonHelper : ICommon
    {
        private readonly IConfiguration _configuration;
        private readonly string _ApiUrl;
        private readonly string _XApiKey;
        private readonly IRethinkMasterDataMicroServices _rethinkServices;
        public CommonHelper(IConfiguration configuration, IRethinkMasterDataMicroServices rethinkServices, IKeyVaultProviderService keyVaultProviderService)
        {
            _configuration = configuration;
            _rethinkServices = rethinkServices;
            _ApiUrl = Convert.ToString(keyVaultProviderService.GetSecretAsync(_configuration["BillingApiUrl"]).Result);
            _XApiKey = Convert.ToString(keyVaultProviderService.GetSecretAsync(_configuration["BillingApiKey"]).Result);
        }

        private static readonly HashSet<BillingClearingHousesEnum> ClearingHousesEnumSet = [BillingClearingHousesEnum.Stedi];

        public async Task<(bool success, string result)> GenerateEDIData(ClearingHouseClaimModel claimModelDto)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("XApiKey", _XApiKey);
                client.BaseAddress = new Uri(_ApiUrl + "/ClearingHouse/GenerateEDIData");
                var content = new StringContent(JsonConvert.SerializeObject(claimModelDto), Encoding.UTF8, "application/json");
                while (true)
                {
                    var response = await client.PostAsync(client.BaseAddress, content);
                    var responseData = await response.Content.ReadAsStringAsync();
                    var result = Regex.Replace(JsonConvert.DeserializeObject<string>(responseData), "[\"“”]", string.Empty);
                    if (response.IsSuccessStatusCode)
                    {
                        return (true, result);
                    }
                    else if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        return (false, result);
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromSeconds(30));
                    }
                }
            }
        }

        public async Task<(bool success, string result)> UploadfileToBlobStorage(ClaimUploadModelWithUserInfo filesWithUserInfo)
        {
            using (var client = CreateHttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("XApiKey", _XApiKey);

                client.BaseAddress = new Uri(_ApiUrl + "/ClearingHouse/UploadFileToBlobStorage");
                var content = new StringContent(JsonConvert.SerializeObject(filesWithUserInfo), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(client.BaseAddress, content);
                if (response.IsSuccessStatusCode)
                {
                    var resultString = await response.Content.ReadAsStringAsync();
                    return (true, resultString);
                }
                else
                {
                    var errormessage = await response.Content.ReadAsStringAsync();
                    return (false, errormessage);
                }

            }
        }

        public async Task<ClearingHouseDetailsModel> GetclearinghouseNameById(int clearinghouseId)
        {
            var clearingHouseEnum = (BillingClearingHousesEnum)clearinghouseId;

             return new ClearingHouseDetailsModel
            {
                ClearingHouseId = clearinghouseId,
                Title = clearingHouseEnum.ToString()
            };
        }

        public async Task<(bool success, string result)> UploadSFTPfilesToBlobStorage(DownloadSftpDataModel fileData)
        {
            using (var client = CreateHttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("XApiKey", _XApiKey);

                client.BaseAddress = new Uri(_ApiUrl + "/ClearingHouse/UploadEDIResponseFile");
                var data = fileData;
                var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(client.BaseAddress, content);
                if (response.IsSuccessStatusCode)
                {
                    var resultString = await response.Content.ReadAsStringAsync();
                    return (true, resultString);
                }
                else if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    var errormessage = await response.Content.ReadAsStringAsync();
                    return (false, errormessage);
                }
                else
                    return (false, null);

            }
        }

        public async Task<bool> ReapplyPRAdjustmentAfterSecondaryBilling(int claimId)
        {
            using (var client = CreateHttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("XApiKey", _XApiKey);

                client.BaseAddress = new Uri(_ApiUrl + "/ServiceLineAdjustment/ReapplyPRAdjustmentAfterSecondaryBilling");
                var content = new StringContent(JsonConvert.SerializeObject(claimId), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(client.BaseAddress, content);
                if (response.IsSuccessStatusCode)
                    return true;
                else
                    return false;

            }
        }

        public async Task<(bool success, string result)> Generate270EDIData(Eligibility270Request eligibility270Request)
        {
            using (var client = CreateHttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("XApiKey", _XApiKey);
                client.BaseAddress = new Uri(_ApiUrl + "/ClearingHouse/Generate270EDIData");
                var content = new StringContent(JsonConvert.SerializeObject(eligibility270Request), Encoding.UTF8, "application/json");
                while (true)
                {
                    var response = await client.PostAsync(client.BaseAddress, content);
                    var responseData = await response.Content.ReadAsStringAsync();
                    var result = Regex.Replace(JsonConvert.DeserializeObject<string>(responseData), "[\"“”]", string.Empty);
                    if (response.IsSuccessStatusCode)
                    {
                        return (true, result);
                    }
                    else if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        return (false, result);
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromSeconds(30));
                    }
                }
            }
        }

        protected virtual HttpClient CreateHttpClient()
        {
            return new HttpClient();
        }
      

    }
}
