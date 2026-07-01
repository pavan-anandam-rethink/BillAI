using ClaimLifecycleMonitoring.Application.Features.Claims.Commands;
using ClaimLifecycleMonitoring.Contracts.Claims;
using FluentValidation;

namespace ClaimLifecycleMonitoring.Application.Validators;

/// <summary>Validator for <see cref="CreateClaimRequest"/> payloads.</summary>
public sealed class CreateClaimRequestValidator : AbstractValidator<CreateClaimCommand>
{
    /// <summary>Configures validation rules.</summary>
    public CreateClaimRequestValidator()
    {
        RuleFor(x => x.Request).NotNull();
        When(x => x.Request is not null, () =>
        {
            RuleFor(x => x.Request.ClaimNumber).NotEmpty().MaximumLength(64);
            RuleFor(x => x.Request.CustomerCode).NotEmpty().MaximumLength(64);
            RuleFor(x => x.Request.CustomerName).NotEmpty().MaximumLength(256);
            RuleFor(x => x.Request.AccountCode).NotEmpty().MaximumLength(64);
            RuleFor(x => x.Request.AccountName).NotEmpty().MaximumLength(256);
            RuleFor(x => x.Request.PatientId).NotEmpty().MaximumLength(64);
            RuleFor(x => x.Request.PatientName).NotEmpty().MaximumLength(256);
            RuleFor(x => x.Request.ProviderId).NotEmpty().MaximumLength(64);
            RuleFor(x => x.Request.ProviderName).NotEmpty().MaximumLength(256);
            RuleFor(x => x.Request.BilledAmount).GreaterThanOrEqualTo(0m);
        });
    }
}

/// <summary>Validator for <see cref="AdvanceClaimStageCommand"/>.</summary>
public sealed class AdvanceClaimStageCommandValidator : AbstractValidator<AdvanceClaimStageCommand>
{
    /// <summary>Configures validation rules.</summary>
    public AdvanceClaimStageCommandValidator()
    {
        RuleFor(x => x.ClaimId).GreaterThan(0);
        RuleFor(x => x.Request).NotNull();
        When(x => x.Request is not null, () =>
        {
            RuleFor(x => x.Request.TargetStage).GreaterThan(0);
            RuleFor(x => x.Request.Message).MaximumLength(1000);
        });
    }
}

/// <summary>Validator for <see cref="RecordFailureCommand"/>.</summary>
public sealed class RecordFailureCommandValidator : AbstractValidator<RecordFailureCommand>
{
    /// <summary>Configures validation rules.</summary>
    public RecordFailureCommandValidator()
    {
        RuleFor(x => x.ClaimId).GreaterThan(0);
        RuleFor(x => x.Request).NotNull();
        When(x => x.Request is not null, () =>
        {
            RuleFor(x => x.Request.FailureCategory).GreaterThan(0);
            RuleFor(x => x.Request.ExceptionMessage).NotEmpty().MaximumLength(4000);
        });
    }
}

/// <summary>Validator for <see cref="CancelClaimCommand"/>.</summary>
public sealed class CancelClaimCommandValidator : AbstractValidator<CancelClaimCommand>
{
    /// <summary>Configures validation rules.</summary>
    public CancelClaimCommandValidator()
    {
        RuleFor(x => x.ClaimId).GreaterThan(0);
        RuleFor(x => x.Request).NotNull();
        When(x => x.Request is not null, () => RuleFor(x => x.Request.Reason).NotEmpty().MaximumLength(1000));
    }
}

/// <summary>Validator for <see cref="ApplyPaymentCommand"/>.</summary>
public sealed class ApplyPaymentCommandValidator : AbstractValidator<ApplyPaymentCommand>
{
    /// <summary>Configures validation rules.</summary>
    public ApplyPaymentCommandValidator()
    {
        RuleFor(x => x.ClaimId).GreaterThan(0);
        RuleFor(x => x.Request).NotNull();
        When(x => x.Request is not null, () => RuleFor(x => x.Request.Amount).GreaterThan(0m));
    }
}

/// <summary>Validator for <see cref="RetryClaimCommand"/>.</summary>
public sealed class RetryClaimCommandValidator : AbstractValidator<RetryClaimCommand>
{
    /// <summary>Configures validation rules.</summary>
    public RetryClaimCommandValidator()
    {
        RuleFor(x => x.ClaimId).GreaterThan(0);
    }
}
