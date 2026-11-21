using Microsoft.AspNetCore.Identity;

namespace TaskAuthenticationAuthorization.Models
{
    public class ApplicationUser : IdentityUser
    {
        public int? CustomerId { get; set; }
    }
}
