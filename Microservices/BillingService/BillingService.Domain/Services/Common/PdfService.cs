using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using BillingService.Domain.Interfaces.Common;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Rethink.Services.Domain.Interfaces;
using RethinkCore.Print.Contracts.Models;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Common
{
    public class PdfService(IConfiguration configuration,IKeyVaultProviderService keyVaultProviderService) : IPdfService
    {

        //public async Task<byte[]> GeneratePDF(string htmlContent)
        //{
        //    try
        //    {
        //        var browserFetcher = new BrowserFetcher();
        //        await browserFetcher.DownloadAsync(); 
        //        var launchOptions = new LaunchOptions
        //        {
        //            Headless = true,
        //            Args = new string[]
        //            {
        //       "--no-sandbox", "--disable-setuid-sandbox", // These options help with Linux permissions
        //       "--disable-dev-shm-usage", // Avoids shared memory issues
        //       "--disable-gpu", // Disables GPU usage, which might not be available in containers
        //       "--disable-software-rasterizer" // Prevents Chrome from trying to use the GPU for rendering
        //            }
        //        };

        //        await using var browser = await Puppeteer.LaunchAsync(launchOptions);
        //        await using var page = await browser.NewPageAsync();
        //        await page.SetContentAsync(htmlContent, new NavigationOptions
        //        {
        //            WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
        //        });
        //        await page.SetViewportAsync(new ViewPortOptions
        //        {
        //            Width = 500,
        //            Height = 500
        //        });
        //        var pdfBytes = await page.PdfDataAsync(new PdfOptions
        //        {
        //            Format = PaperFormat.A4,
        //            PrintBackground = true,
        //            MarginOptions = new MarginOptions
        //            {
        //                Top = "20px",
        //                Right = "20px",
        //                Bottom = "20px",
        //                Left = "20px"
        //            }
        //        });
        //        return pdfBytes;
        //    }
        //    catch (Exception ex)
        //    {
        //        // Improved error handling
        //        throw new Exception($"Error generating PDF: {ex.Message}", ex);
        //    }
        //}

        public async Task<byte[]> GeneratePDF(string htmlContent)
        {
            var keyVaultClient = new SecretClient(new Uri(configuration["KeyVaultUri"]), new DefaultAzureCredential());

            var rethinkPrintClientId = keyVaultProviderService.GetSecretAsync(configuration["RethinkPrintClientId"]).Result;
            var rethinkPrintTenantId = keyVaultProviderService.GetSecretAsync(configuration["RethinkPrintTenantId"]).Result;
            var rethinkPrintSecret = keyVaultProviderService.GetSecretAsync(configuration["RethinkPrintSecret"]).Result;
            var rethinkPrintScopes = configuration["RethinkPrintScopes"];
            var rethinkPrintAPI = configuration["RethinkPrintAPI"];

            var clientSecretCredential = new ClientSecretCredential(rethinkPrintTenantId, rethinkPrintClientId, rethinkPrintSecret);


            // Get tokens
            var token = await clientSecretCredential.GetTokenAsync(new TokenRequestContext([rethinkPrintScopes]));

            var req = new GetHtmlToPdfConverterRequest
            {
                HtmlBody = htmlContent
            };
            req.PdfOptions.Landscape = false;
            req.PdfOptions.Format = "A4";

            var client = new HttpClient
            {
                BaseAddress = new Uri(configuration["RethinkPrintAPI"])
            };
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
            StringContent content = new(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync("api/HtmlToPdfConverter", content);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            else
            {
                return [];
            }
        }
    }
}
