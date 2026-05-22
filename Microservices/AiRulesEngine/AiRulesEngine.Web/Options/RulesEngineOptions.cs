namespace AiRulesEngine.Web.Options
{
    public class RulesEngineStorageOptions
    {
        public string FilePath { get; set; } = "data/rule-sets.json";
    }

    public class RulesEngineAiOptions
    {
        public string Endpoint { get; set; }
        public string ApiKey { get; set; }
        public string Model { get; set; } = "gpt-4.1";
        public double Temperature { get; set; } = 0.1;
        public string PromptTemplatePath { get; set; } = "Prompts/rule-extraction.md";
    }
}
