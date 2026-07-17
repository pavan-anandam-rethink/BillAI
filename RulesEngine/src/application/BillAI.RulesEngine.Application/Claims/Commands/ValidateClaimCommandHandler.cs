using System.Text.Json;
using BillAI.RulesEngine.Application.Interfaces.Repositories;
using BillAI.RulesEngine.Application.Interfaces.Services;
using BillAI.RulesEngine.Domain.Entities.Claims;
using BillAI.RulesEngine.Shared.Models;
using MediatR;

namespace BillAI.RulesEngine.Application.Claims.Commands;

/// <summary>Handles <see cref="ValidateClaimCommand"/>.</summary>
public sealed class ValidateClaimCommandHandler(
    IRuleRepository ruleRepository,
    IClaimValidationRepository claimValidationRepository,
    IRulesEvaluationEngine evaluationEngine)
    : IRequestHandler<ValidateClaimCommand, Result<ClaimValidationResponse>>
{
    public async Task<Result<ClaimValidationResponse>> Handle(
        ValidateClaimCommand request,
        CancellationToken cancellationToken)
    {
        // Retrieve all published rules for this tenant
        var rules = await ruleRepository.GetPublishedRulesAsync(
            request.TenantId,
            request.Category,
            cancellationToken);

        // Deserialise the claim payload
        object dataContext;
        try
        {
            dataContext = JsonSerializer.Deserialize<JsonElement>(request.ClaimJson);
        }
        catch (JsonException ex)
        {
            return Result.Failure<ClaimValidationResponse>($"Invalid claim JSON: {ex.Message}");
        }

        // Run the evaluation engine
        var evalResult = await evaluationEngine.EvaluateAsync(rules, dataContext, cancellationToken);

        // Persist audit record
        var record = ClaimValidationRecord.Create(request.TenantId, request.ClaimId, request.ClaimJson, request.RequestedBy);
        record.RecordResult(
            evalResult.IsValid,
            evalResult.PassedRules.Count,
            evalResult.FailedRules.Count,
            evalResult.Warnings.Count,
            evalResult.ExecutionTimeMs,
            JsonSerializer.Serialize(evalResult));

        await claimValidationRepository.AddAsync(record, cancellationToken);

        return Result.Success(new ClaimValidationResponse
        {
            AuditId = record.AuditId,
            IsValid = evalResult.IsValid,
            PassedRules = evalResult.PassedRules,
            FailedRules = evalResult.FailedRules,
            Warnings = evalResult.Warnings,
            ExecutionTimeMs = evalResult.ExecutionTimeMs,
            DecisionTrace = evalResult.DecisionTrace
        });
    }
}
