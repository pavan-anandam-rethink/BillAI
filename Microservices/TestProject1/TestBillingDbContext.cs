using Microsoft.EntityFrameworkCore;
using Rethink.Services.Common.Infrastructure.Context.Billing;

public class TestBillingDbContext : BillingDbContext
{
    public TestBillingDbContext(DbContextOptions<BillingDbContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        //BillingDbContext uses SQL Server by default, override to use InMemory for tests
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseInMemoryDatabase("FakeBillingDb");
        }
    }
}
