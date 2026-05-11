using ClearingHouse.SharedKernel.Enums;
using ClearingHouse.SharedKernel.Models;

namespace ClearinghousePlugins.Abstractions;

public interface IClearinghousePlugin
{
    ClearinghouseType ClearinghouseType { get; }
    string Name { get; }
    Task<Result> InitializeAsync(ClearinghouseConnectionConfig config, CancellationToken cancellationToken = default);
    Task<Result> TestConnectionAsync(CancellationToken cancellationToken = default);
}
