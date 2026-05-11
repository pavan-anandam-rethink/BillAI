using ClearingHouse.SharedKernel.Models;
using EdiProcessing.Domain.Entities;

namespace EdiProcessing.Domain.Interfaces;

public interface IEdiValidator
{
    Task<Result> ValidateAsync(EdiDocument document, CancellationToken cancellationToken = default);
}
