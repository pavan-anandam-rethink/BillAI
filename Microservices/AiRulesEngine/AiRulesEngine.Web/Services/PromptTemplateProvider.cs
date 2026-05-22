using AiRulesEngine.Web.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AiRulesEngine.Web.Services
{
    public class PromptTemplateProvider
    {
        private readonly RulesEngineAiOptions _options;
        private readonly ILogger<PromptTemplateProvider> _logger;

        public PromptTemplateProvider(IOptions<RulesEngineAiOptions> options, ILogger<PromptTemplateProvider> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task<string> GetPromptAsync()
        {
            var path = ResolvePath();
            if (!File.Exists(path))
            {
                _logger.LogWarning("Prompt template not found at {Path}", path);
                return string.Empty;
            }

            return await File.ReadAllTextAsync(path);
        }

        private string ResolvePath()
        {
            return Path.IsPathRooted(_options.PromptTemplatePath)
                ? _options.PromptTemplatePath
                : Path.Combine(AppContext.BaseDirectory, _options.PromptTemplatePath);
        }
    }
}
