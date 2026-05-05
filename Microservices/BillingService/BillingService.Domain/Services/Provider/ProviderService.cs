using AutoMapper;
using BillingService.Domain.Interfaces.Provider;
using BillingService.Domain.Models.Clients;
using BillingService.Domain.Services.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Services;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Provider
{
    public class ProviderService : BaseService, IProviderService
    {

        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private IConfiguration _config;
        private readonly string _PracticeOperationsApiKey;
        private readonly string _headerKey;
        private readonly string _PracticeOperationsApiUrl;
        private readonly IKeyVaultProviderService _keyVaultProviderService;

        public ProviderService(
            IConfiguration configuration,          
            ILogger<CommonService> logger,
            IConfiguration config,
            IKeyVaultProviderService keyVaultProviderService)
        {
            _configuration = configuration;
            _logger = logger;
         
            _config = config;
            _keyVaultProviderService = keyVaultProviderService;
            _PracticeOperationsApiUrl =Convert.ToString(_configuration.GetSection("PracticeOperationsApiUrl").Value);
            _PracticeOperationsApiKey = _keyVaultProviderService.GetSecretAsync(Convert.ToString(_configuration.GetSection("PracticeOperationsKey").Value)).Result;
            _headerKey = Convert.ToString(_configuration.GetSection("HeaderKey").Value);
        }


        public async Task<List<ProviderLocationData>> GetProviderLocationList(int accountInfoId, JsonSerializerSettings settings)
        {
            try
            {
                var response = string.Empty;
                var resDemographics = new ClientProviderLocationsModel();
                var resUri = _PracticeOperationsApiUrl + "/accounts/" + accountInfoId + "/providerLocations";

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add(_headerKey, _PracticeOperationsApiKey);
                    HttpResponseMessage Status = await client.GetAsync(resUri);
                    if (Status.IsSuccessStatusCode)
                    {
                        response = await Status.Content.ReadAsStringAsync();
                        resDemographics = JsonConvert.DeserializeObject<ClientProviderLocationsModel>(response, settings);
                    }

                }
                var result = resDemographics.data
                    .Select(x => new ProviderLocationData
                    {
                        Id = x.id,
                        Name = x.name,
                        IsMainLocation = x.isMainLocation,
                        IsBillingLocation = x.isBillingLocation,
                        AgencyName = x.agencyName

                    }).OrderBy(x => x.Name).ToList();

                return result;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}
