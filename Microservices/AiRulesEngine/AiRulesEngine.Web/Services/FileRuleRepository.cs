using AiRulesEngine.Web.Models;
using AiRulesEngine.Web.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AiRulesEngine.Web.Services
{
    public class FileRuleRepository : IRuleRepository
    {
        private readonly RulesEngineStorageOptions _options;
        private readonly ILogger<FileRuleRepository> _logger;
        private readonly SemaphoreSlim _mutex = new(1, 1);

        public FileRuleRepository(IOptions<RulesEngineStorageOptions> options, ILogger<FileRuleRepository> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        public async Task<IReadOnlyList<RuleSet>> ListAsync(CancellationToken cancellationToken = default)
        {
            var ruleSets = await LoadAsync(cancellationToken);
            return ruleSets;
        }

        public async Task<RuleSet> GetAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            var ruleSets = await LoadAsync(cancellationToken);
            return ruleSets.FirstOrDefault(rs => string.Equals(rs.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        public async Task SaveAsync(RuleSet ruleSet, CancellationToken cancellationToken = default)
        {
            if (ruleSet == null)
            {
                throw new ArgumentNullException(nameof(ruleSet));
            }

            await _mutex.WaitAsync(cancellationToken);
            try
            {
                var ruleSets = await LoadInternalAsync(cancellationToken);
                var existing = ruleSets.FirstOrDefault(rs => string.Equals(rs.Id, ruleSet.Id, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    ruleSets.Remove(existing);
                }

                ruleSets.Add(ruleSet);
                await SaveInternalAsync(ruleSets, cancellationToken);
            }
            finally
            {
                _mutex.Release();
            }
        }

        private async Task<List<RuleSet>> LoadAsync(CancellationToken cancellationToken)
        {
            await _mutex.WaitAsync(cancellationToken);
            try
            {
                return await LoadInternalAsync(cancellationToken);
            }
            finally
            {
                _mutex.Release();
            }
        }

        private async Task<List<RuleSet>> LoadInternalAsync(CancellationToken cancellationToken)
        {
            var path = ResolvePath();
            if (!File.Exists(path))
            {
                return new List<RuleSet>();
            }

            try
            {
                var json = await File.ReadAllTextAsync(path, cancellationToken);
                return JsonSerializer.Deserialize<List<RuleSet>>(json, SerializerOptions) ?? new List<RuleSet>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load rule sets from {Path}", path);
                return new List<RuleSet>();
            }
        }

        private async Task SaveInternalAsync(List<RuleSet> ruleSets, CancellationToken cancellationToken)
        {
            var path = ResolvePath();
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");

            var json = JsonSerializer.Serialize(ruleSets, SerializerOptions);
            await File.WriteAllTextAsync(path, json, cancellationToken);
        }

        private string ResolvePath()
        {
            return Path.IsPathRooted(_options.FilePath)
                ? _options.FilePath
                : Path.Combine(AppContext.BaseDirectory, _options.FilePath);
        }
    }
}
