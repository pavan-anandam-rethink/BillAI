using AiRulesEngine.Web.Models;
using AiRulesEngine.Web.Options;
using AiRulesEngine.Web.Services;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.Configure<RuleEngineOptions>(builder.Configuration.GetSection("RuleEngine"));
builder.Services.Configure<AiProviderOptions>(builder.Configuration.GetSection("AiProvider"));

builder.Services.AddSingleton<IFileTextExtractor, RuleFileTextExtractor>();
builder.Services.AddSingleton<JsonRuleSetParser>();
builder.Services.AddSingleton<IAiRuleParser, OpenAiRuleParser>();
builder.Services.AddSingleton<IRuleSetParser, CompositeRuleSetParser>();
builder.Services.AddSingleton<IRuleSetStore, FileRuleSetStore>();
builder.Services.AddSingleton<IRuleEvaluator, JsonRuleEvaluator>();
builder.Services.AddSingleton<RuleSetIngestionService>();

builder.Services.AddHttpClient<OpenAiRuleParser>();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/rulesets/ingest", async (
    IFormFile file,
    string? name,
    RuleSetIngestionService ingestionService,
    CancellationToken cancellationToken) =>
{
    if (file == null || file.Length == 0)
    {
        return Results.BadRequest("A rules file is required.");
    }

    var ruleSet = await ingestionService.IngestAsync(file, name, cancellationToken);
    return Results.Ok(new RuleSetSummary
    {
        Id = ruleSet.Id,
        Name = ruleSet.Name,
        Version = ruleSet.Version,
        RuleCount = ruleSet.Rules.Count,
        WorkflowRuleCount = ruleSet.WorkflowRules.Count
    });
});

app.MapPost("/api/rulesets/ingest-text", async (
    RuleTextIngestionRequest request,
    RuleSetIngestionService ingestionService,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Content))
    {
        return Results.BadRequest("Rules content is required.");
    }

    var ruleSet = await ingestionService.IngestTextAsync(request.Content, request.Name, cancellationToken);
    return Results.Ok(new RuleSetSummary
    {
        Id = ruleSet.Id,
        Name = ruleSet.Name,
        Version = ruleSet.Version,
        RuleCount = ruleSet.Rules.Count,
        WorkflowRuleCount = ruleSet.WorkflowRules.Count
    });
});

app.MapGet("/api/rulesets/{ruleSetId}", async (
    string ruleSetId,
    IRuleSetStore store,
    CancellationToken cancellationToken) =>
{
    var ruleSet = await store.GetAsync(ruleSetId, cancellationToken);
    return ruleSet == null ? Results.NotFound() : Results.Ok(ruleSet);
});

app.MapPost("/api/validate", async (
    RuleValidationRequest request,
    IRuleSetStore store,
    IRuleEvaluator evaluator,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.RuleSetId))
    {
        return Results.BadRequest("RuleSetId is required.");
    }

    var ruleSet = await store.GetAsync(request.RuleSetId, cancellationToken);
    if (ruleSet == null)
    {
        return Results.NotFound();
    }

    var violations = evaluator.Evaluate(ruleSet, request.Payload);
    var workflowActions = evaluator.EvaluateWorkflows(ruleSet, request.Payload);
    return Results.Ok(new RuleValidationResponse
    {
        RuleSetId = ruleSet.Id,
        Violations = violations.ToList(),
        WorkflowActions = workflowActions.ToList()
    });
});

app.Run();
