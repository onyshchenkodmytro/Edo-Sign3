using Duende.IdentityServer.Models;
using System.Collections.Generic;

public static class Config
{
    // =====================================================
    // === Identity ресурси (що включаються в ID Token) ====
    // =====================================================
    public static IEnumerable<IdentityResource> IdentityResources =>
        new IdentityResource[]
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
            new IdentityResources.Email(),

            // ✅ Власний ресурс із логіном користувача
            new IdentityResource
            {
                Name = "custom_profile",
                DisplayName = "Custom user profile",
                UserClaims = { "preferred_username", "name" }
            }
        };

    // =====================================================
    // === API Scopes ======================================
    // =====================================================
    public static IEnumerable<ApiScope> ApiScopes => new[]
    {
        new ApiScope("edolab.api", "EdoSign API")
    };

    // =====================================================
    // === Клієнти, що підключаються до IdentityServer =====
    // =====================================================
    public static IEnumerable<Client> Clients => new[]
    {
        new Client
        {
            ClientId = "mvc",
            ClientName = "EdoSign MVC",

            // OpenID Connect + PKCE
            AllowedGrantTypes = GrantTypes.Code,
            RequirePkce = false,
            RequireClientSecret = true,
            ClientSecrets = { new Secret("secret".Sha256()) },

            
            RedirectUris =           { "http://localhost:7275/signin-oidc" },
            PostLogoutRedirectUris = { "http://localhost:7275/signout-callback-oidc" },
            FrontChannelLogoutUri =  "http://localhost:7275/signout-oidc",

            // Scopes, які клієнт може запитувати
            AllowedScopes = { "openid", "profile", "email", "custom_profile", "edolab.api" },


            AllowOfflineAccess = true,
            AlwaysIncludeUserClaimsInIdToken = true
        }
    };
}
