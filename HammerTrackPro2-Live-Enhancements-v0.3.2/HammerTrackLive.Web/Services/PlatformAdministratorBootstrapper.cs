using HammerTrackLive.Web.Data;
using Microsoft.AspNetCore.Identity;

namespace HammerTrackLive.Web.Services;

public static class PlatformAdministratorBootstrapper
{
    public static async Task<int> RunAsync(IServiceProvider services, IConfiguration configuration)
    {
        var email = configuration["Bootstrap:Email"]?.Trim();
        var displayName = configuration["Bootstrap:DisplayName"]?.Trim();
        var password = configuration["Bootstrap:Password"];
        if (string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(displayName)
            || string.IsNullOrWhiteSpace(password))
        {
            Console.Error.WriteLine("Bootstrap email, display name, and password must be supplied through environment configuration.");
            return 2;
        }

        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = displayName,
                CreatedUtc = DateTimeOffset.UtcNow,
                IsActive = true
            };
            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                Console.Error.WriteLine(string.Join(" ", createResult.Errors.Select(error => error.Description)));
                return 3;
            }
        }

        if (!user.EmailConfirmed || !user.IsActive)
        {
            user.EmailConfirmed = true;
            user.IsActive = true;
            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                Console.Error.WriteLine(string.Join(" ", updateResult.Errors.Select(error => error.Description)));
                return 4;
            }
        }

        if (!await userManager.IsInRoleAsync(user, PlatformRoles.Administrator))
        {
            var roleResult = await userManager.AddToRoleAsync(user, PlatformRoles.Administrator);
            if (!roleResult.Succeeded)
            {
                Console.Error.WriteLine(string.Join(" ", roleResult.Errors.Select(error => error.Description)));
                return 5;
            }
        }

        Console.WriteLine("Platform administrator bootstrap completed.");
        return 0;
    }
}
