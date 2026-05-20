using IdentityService.Persistence.Context;
using Microsoft.Extensions.Logging;

namespace IdentityService.Persistence.Seed;

public static class IdentityDbContextSeed
{
    public static async Task SeedAsync(IdentityDbContext context, ILogger logger)
    {
        if (!context.UserProfiles.Any())
        {
            logger.LogInformation("No seed data required for Identity Service");
        }

        await Task.CompletedTask;
    }
}
