using AiRulesEngine.Web.Models;
using AiRulesEngine.Web.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AiRulesEngine.Web.Services
{
    public class OpenAiRuleExtractor : IRuleExtractor
    {
        private readonly HttpClient _httpClient;
        private readonly RulesEngineAiOptions _options;
        private readonly PromptTemplateProvider _promptProvider;
        private readonly ILogger<OpenAiRuleExtractor> _logger;

        public OpenAiRuleExtractor(
            HttpClient httpClient,
            IOptions<RulesEngineAiOptions> options,
            PromptTemplateProvider promptProvider,
            ILogger<OpenAiRuleExtractor> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _promptProvider = promptProvider;
            _logger = logger;
        }

        public async Task<IReadOnlyList<RuleDefinition>> ExtractRulesAsync(string content, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.Endpoint) || string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                throw new InvalidOperationException("Rules engine AI endpoint or API key is missing.");
            }

            var systemPrompt = await _promptProvider.GetPromptAsync();

            var requestBody = new OpenAiChatRequest
            {
                Model = _options.Model,
                Temperature = _options.Temperature,
                Messages = new List<OpenAiChatMessage>
                {
                    new OpenAiChatMessage { Role = "system", Content = systemPrompt },
                    new OpenAiChatMessage { Role = "user", Content = content }
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            request.Content = JsonContent.Create(requestBody);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("AI extraction failed. Status={Status} Body={Body}", response.StatusCode, error);
                throw new InvalidOperationException("AI extraction failed.");
            }

            var chatResponse = await response.Content.ReadFromJsonAsync<OpenAiChatResponse>(cancellationToken: cancellationToken);
            var contentPayload = chatResponse?.Choices?[0]?.Message?.Content;
            if (string.IsNullOrWhiteSpace(contentPayload))
            {
                return Array.Empty<RuleDefinition>();
            }

            var json = ExtractJson(contentPayload);
            return JsonSerializer.Deserialize<List<RuleDefinition>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            }) ?? Array.Empty<RuleDefinition>();
        }

        private static string ExtractJson(string content)
        {
            var trimmed = content.Trim();
            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                var startIndex = trimmed.IndexOf('\n');
                var endIndex = trimmed.LastIndexOf("```", StringComparison.Ordinal);
                if (startIndex >= 0 && endIndex > startIndex)
                {
                    trimmed = trimmed.Substring(startIndex, endIndex - startIndex).Trim();
                }
            }

            return trimmed;
        }

        private class OpenAiChatRequest
        {
            public string Model { get; set; }
            public double Temperature { get; set; }
            public List<OpenAiChatMessage> Messages { get; set; }
        }

        private class OpenAiChatMessage
        {
            public string Role { get; set; }
            public string Content { get; set; }
        }

        private class OpenAiChatResponse
        {
            public List<OpenAiChatChoice> Choices { get; set; }
        }

        private class OpenAiChatChoice
        {
            public OpenAiChatMessage Message { get; set; }
        }
    }
}
