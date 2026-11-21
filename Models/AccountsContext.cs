using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace TaskAuthenticationAuthorization.Models
{
    public class AccountsContext : IdentityDbContext<ApplicationUser>
    {
        public AccountsContext(DbContextOptions<AccountsContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<ApplicationUser>().HasIndex(user => user.CustomerId).IsUnique();
        }
    }
}
