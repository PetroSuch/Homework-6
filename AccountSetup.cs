using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskAuthenticationAuthorization.Models;

namespace TaskAuthenticationAuthorization
{
    public static class AccountSetup
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            var context = services.GetRequiredService<AccountsContext>();
            await context.Database.EnsureCreatedAsync();

            var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
            foreach (var role in new[] { "Admin", "Buyer" })
            {
                if (!await roles.RoleExistsAsync(role))
                {
                    Check(await roles.CreateAsync(new IdentityRole(role)));
                }
            }

            var configuration = services.GetRequiredService<IConfiguration>();
            var email = configuration["InitialAdmin:Email"];
            var password = configuration["InitialAdmin:Password"];
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            var users = services.GetRequiredService<UserManager<ApplicationUser>>();
            if ((await users.GetUsersInRoleAsync("Admin")).Count > 0)
            {
                return;
            }

            if (await users.FindByEmailAsync(email) != null)
            {
                throw new InvalidOperationException("The initial admin email already belongs to an account. Choose another email.");
            }

            await using var transaction = await context.Database.BeginTransactionAsync();
            var admin = new ApplicationUser { UserName = email, Email = email };
            Check(await users.CreateAsync(admin, password));
            Check(await users.AddToRoleAsync(admin, "Admin"));
            await transaction.CommitAsync();
        }

        private static void Check(IdentityResult result)
        {
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join(" ", result.Errors.Select(error => error.Description)));
            }
        }
    }
}
