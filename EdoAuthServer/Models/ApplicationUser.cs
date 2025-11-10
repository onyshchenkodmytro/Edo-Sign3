using Microsoft.AspNetCore.Identity;

namespace EdoAuthServer.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
    }
}
