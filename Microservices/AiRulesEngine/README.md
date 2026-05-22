# AI Rules Engine

The AI Rules Engine service ingests rule documents (text or PDF), extracts validation rules using an LLM, stores them in a structured DSL, and evaluates claims against those rules.

## Configuration

`appsettings.json` supports the following keys:

- `RulesEngine:DefaultRuleSetId`: Default ruleset used when the caller does not specify a ruleset.
- `RulesEngine:Storage:FilePath`: JSON file path for persisted rule sets.
- `RulesEngine:Ai:Endpoint`: OpenAI-compatible chat completions endpoint.
- `RulesEngine:Ai:ApiKey`: API key for the LLM endpoint.
- `RulesEngine:Ai:Model`: Model name (default `gpt-4.1`).
- `RulesEngine:Ai:Temperature`: Sampling temperature (default `0.1`).
- `RulesEngine:Ai:PromptTemplatePath`: Prompt template file path.

### BillingService integration

BillingService calls the rules engine when the feature flag `EnableAiClaimRulesEngine` is enabled in Key Vault. Configure these settings in `BillingService.Web`:

```
RulesEngine:BaseUrl
RulesEngine:DefaultRuleSetId
RulesEngine:ValidationEndpoint (default: api/validation/claim)
```

## API Endpoints

- `POST /api/rules/ingest` (multipart/form-data)
  - Form fields: `file` (PDF or text), `ruleSetName` (optional), `ruleSetId` (optional), `ruleSetDescription` (optional)
- `GET /api/rulesets`
- `GET /api/rulesets/{id}`
- `POST /api/validation/claim`
  - Body: `ClaimRulesEngineRequest` with `RuleSetId` and `Context`

## Rule DSL

Each rule is stored as:

```
{
  "Id": "rule-1",
  "Name": "Billing Provider ZIP",
  "Message": "Billing provider ZIP must be 9 digits.",
  "Severity": "Error",
  "MatchMode": "All",
  "Conditions": [
    { "Field": "BillingProviderAddress.Zip", "Operator": "LengthEquals", "Value": "9", "Values": [] },
    { "Field": "BillingProviderAddress.Zip", "Operator": "Numeric", "Value": "", "Values": [] }
  ]
}
```

Supported operators: `Required`, `Equals`, `NotEquals`, `GreaterThan`, `LessThan`, `Regex`, `Numeric`, `LengthEquals`, `LengthMin`, `LengthMax`, `In`.

## Prompt Template

The prompt template is stored at:

```
Microservices/AiRulesEngine/AiRulesEngine.Web/Prompts/rule-extraction.md
```

## Samples

Sample rule documents are stored in:

```
docs/ai-rules-engine/samples
```

## Testing strategy

- Unit tests for rule evaluation and file text extraction live in `Microservices/Test/AiRulesEngineTests`.
- Claim validation helper methods for ZIP, eligibility recency, and enrollment checks are covered in `ClaimValidationServiceTest`.
- Use the sample PDF/text files under `docs/ai-rules-engine/samples` to regression-test ingestion flows.

## Running locally

```
dotnet run --project Microservices/AiRulesEngine/AiRulesEngine.Web
```

## Deployment

- Dockerfile: `Microservices/AiRulesEngine/AiRulesEngine.Web/Dockerfile`
- Helm chart: `helm/ai-rules-engine`
- K8s manifests: `k8s/ai-rules-engine`
