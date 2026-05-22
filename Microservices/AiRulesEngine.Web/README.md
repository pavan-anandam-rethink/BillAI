# AI Rules Engine

AI Rules Engine is a lightweight validation service that ingests rule definitions from text or PDF files, converts them into a structured ruleset, and validates JSON payloads against those rules. It supports both static validation rules and workflow-style actions.

## Features
- File ingestion for text or PDF rule sources.
- JSON ruleset storage and retrieval.
- Validation endpoint for rules + payloads.
- AI-assisted rule parsing with a configurable prompt.

## Endpoints
- `POST /api/rulesets/ingest` — Upload a rules document (`multipart/form-data` with `file`, optional `name`).
- `POST /api/rulesets/ingest-text` — Submit raw rules text as JSON (`{ \"content\": \"...\", \"name\": \"...\" }`).
- `GET /api/rulesets/{ruleSetId}` — Fetch a stored ruleset.
- `POST /api/validate` — Validate a payload against a ruleset.
- `GET /healthz` — Health check.

## Rule Schema (summary)
Rulesets follow this structure:
- `RuleSetDefinition` → `rules[]` + `workflowRules[]`.
- `RuleDefinition` fields:
  - `severity`: `Error` or `Alert`
  - `appliesWhen[]`: optional preconditions
  - `conditions[]`: required validation logic
- `RuleCondition` operators: `Required`, `Equals`, `NotEquals`, `Regex`, `Numeric`, `LengthEquals`, `LengthBetween`, `DateWithinDays`.

See the sample ruleset at `samples/claim-validation-rules.json`.

## AI Parsing Prompt
The prompt template lives at `prompts/rule-parser.txt`. Customize it to enforce your own rule naming, metadata, or validation patterns. The parser expects the AI response to be **JSON only** matching the schema.

## Configuration
`appsettings.json`:
```json
{
  "RuleEngine": {
    "StoragePath": "data/rulesets",
    "PromptPath": "prompts/rule-parser.txt"
  },
  "AiProvider": {
    "Enabled": false,
    "Endpoint": "https://api.openai.com/v1/chat/completions",
    "Model": "gpt-4.1-mini",
    "ApiKey": "",
    "ApiKeyHeader": "Authorization",
    "ApiKeyPrefix": "Bearer"
  }
}
```

## Running Locally
```bash
cd Microservices/AiRulesEngine.Web

dotnet run
```

## Docker
```bash
docker build -f Microservices/AiRulesEngine.Web/Dockerfile -t ai-rules-engine .
```

## Testing Strategy
- **Unit tests**: rule evaluator conditions, path resolution, date logic, and parser fallbacks.
- **Integration tests**: ingest → store → validate workflow with sample rulesets and payloads.
- **AI contract tests**: snapshot AI output and assert it matches schema.
- **Security tests**: validate file size limits and reject unexpected MIME types.

## Sample Payload
```json
{
  "claim": { "createdDate": "2026-05-01" },
  "account": { "showEligibility": true },
  "eligibility": { "lastCheckDate": "2026-04-10" },
  "enrollment": { "837pRequired": true, "837pCompleted": false },
  "billingProvider": { "zip": "123456789" }
}
```
