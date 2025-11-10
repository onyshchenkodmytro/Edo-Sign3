using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using EdoAuthServer.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace EdoAuthServer.Services
{
    public class ProfileService : IProfileService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            var user = await _userManager.GetUserAsync(context.Subject);
            if (user == null) return;

            // Формуємо набір claims, який буде в токені
            var claims = new List<Claim>
            {
                new Claim("sub", user.Id),
                new Claim("name", user.UserName ?? ""),
                new Claim("preferred_username", user.UserName ?? ""),
                new Claim("email", user.Email ?? ""),
                new Claim("fullname", user.FullName ?? "")
            };

            context.IssuedClaims.AddRange(claims);
        }

        public async Task IsActiveAsync(IsActiveContext context)
        {
            var user = await _userManager.GetUserAsync(context.Subject);
            context.IsActive = user != null;
        }
    }
}

