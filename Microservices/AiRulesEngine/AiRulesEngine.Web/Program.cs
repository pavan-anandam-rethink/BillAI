using AiRulesEngine.Web.Models;
using AiRulesEngine.Web.Options;
using AiRulesEngine.Web.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.Configure<RulesEngineStorageOptions>(builder.Configuration.GetSection("RulesEngine:Storage"));
builder.Services.Configure<RulesEngineAiOptions>(builder.Configuration.GetSection("RulesEngine:Ai"));

builder.Services.AddSingleton<PromptTemplateProvider>();
builder.Services.AddSingleton<IRuleRepository, FileRuleRepository>();
builder.Services.AddSingleton<IFileTextExtractor, FileTextExtractor>();
builder.Services.AddSingleton<NoOpRuleExtractor>();
builder.Services.AddHttpClient<OpenAiRuleExtractor>();
builder.Services.AddSingleton<IRuleExtractor>(sp =>
{
    var options = sp.GetRequiredService<IOptions<RulesEngineAiOptions>>().Value;
    return string.IsNullOrWhiteSpace(options.Endpoint) || string.IsNullOrWhiteSpace(options.ApiKey)
        ? sp.GetRequiredService<NoOpRuleExtractor>()
        : sp.GetRequiredService<OpenAiRuleExtractor>();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/api/rulesets", async (IRuleRepository repository) =>
{
    var ruleSets = await repository.ListAsync();
    var response = ruleSets.Select(set => new
    {
        set.Id,
        set.Name,
        set.Description,
        set.SourceFileName,
        set.CreatedAtUtc,
        RuleCount = set.Rules?.Count ?? 0
    });

    return Results.Ok(response);
});

app.MapGet("/api/rulesets/{id}", async (string id, IRuleRepository repository) =>
{
    var ruleSet = await repository.GetAsync(id);
    return ruleSet == null ? Results.NotFound() : Results.Ok(ruleSet);
});

app.MapPost("/api/rules/ingest", async (HttpRequest request,
                                         IRuleExtractor extractor,
                                         IRuleRepository repository,
                                         IFileTextExtractor fileTextExtractor) =>
{
    var form = await request.ReadFormAsync();
    var file = form.Files.FirstOrDefault();
    if (file == null)
    {
        return Results.BadRequest("No file provided.");
    }

    var rulesText = await fileTextExtractor.ExtractAsync(file);
    if (string.IsNullOrWhiteSpace(rulesText))
    {
        return Results.BadRequest("Unable to extract text from the file.");
    }

    var ruleSetName = form["ruleSetName"].FirstOrDefault() ?? Path.GetFileNameWithoutExtension(file.FileName);
    var ruleSetId = form["ruleSetId"].FirstOrDefault();

    IReadOnlyList<RuleDefinition> rules;
    try
    {
        rules = await extractor.ExtractRulesAsync(rulesText);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: StatusCodes.Status422UnprocessableEntity);
    }

    var ruleSet = new RuleSet
    {
        Id = string.IsNullOrWhiteSpace(ruleSetId) ? Guid.NewGuid().ToString("N") : ruleSetId,
        Name = ruleSetName,
        Description = form["ruleSetDescription"].FirstOrDefault(),
        SourceFileName = file.FileName,
        Rules = rules.ToList()
    };

    await repository.SaveAsync(ruleSet);
    return Results.Ok(ruleSet);
});

app.MapPost("/api/validation/claim", async (ClaimRulesEngineRequest request,
                                           IRuleRepository repository,
                                           IConfiguration configuration) =>
{
    if (request == null)
    {
        return Results.BadRequest("Request payload is required.");
    }

    if (string.IsNullOrWhiteSpace(request.RuleSetId))
    {
        request.RuleSetId = configuration["RulesEngine:DefaultRuleSetId"];
    }

    if (string.IsNullOrWhiteSpace(request.RuleSetId))
    {
        return Results.BadRequest("RuleSetId is required.");
    }

    var ruleSet = await repository.GetAsync(request.RuleSetId);
    if (ruleSet == null)
    {
        return Results.NotFound();
    }

    var violations = RuleEvaluator.Evaluate(ruleSet, request.Context);
    return Results.Ok(violations);
});

app.MapGet("/health/live", () => Results.Ok("ok"));
app.MapGet("/health/ready", () => Results.Ok("ok"));

app.Run();
