using ClaimLifecycleMonitoring.Application.Abstractions.Persistence;
using ClaimLifecycleMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClaimLifecycleMonitoring.Persistence;

/// <summary>
/// EF Core <see cref="DbContext"/> for the claim lifecycle monitoring platform.
/// </summary>
public sealed class ClaimLifecycleDbContext : DbContext, IUnitOfWork
{
    /// <summary>Default schema for the monitoring tables.</summary>
    public const string Schema = "monitoring";

    /// <summary>Creates a new <see cref="ClaimLifecycleDbContext"/>.</summary>
    public ClaimLifecycleDbContext(DbContextOptions<ClaimLifecycleDbContext> options) : base(options)
    {
    }

    /// <summary>Claims aggregate root.</summary>
    public DbSet<Claim> Claims => Set<Claim>();

    /// <summary>Claim lifecycle history events.</summary>
    public DbSet<ClaimLifecycleEvent> LifecycleEvents => Set<ClaimLifecycleEvent>();

    /// <summary>Claim alerts.</summary>
    public DbSet<ClaimAlert> Alerts => Set<ClaimAlert>();

    /// <summary>Per-stage SLA configuration.</summary>
    public DbSet<StageSlaConfiguration> StageSlaConfigurations => Set<StageSlaConfiguration>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClaimLifecycleDbContext).Assembly);
    }

    /// <inheritdoc />
    Task<int> IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken)
        => SaveChangesAsync(cancellationToken);
}
