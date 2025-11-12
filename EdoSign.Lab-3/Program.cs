using EdoSign.Lab_3.Data;
using EdoSign.Lab_3.Models;
using EdoSign.Signing;
using EdoSign.Lab_3.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// =======================================================
// 🔹 Додай цей рядок одразу після створення builder!
// =======================================================
AppContext.SetSwitch("Microsoft.AspNetCore.Authentication.SuppressSameSiteNone", true);

// =======================================================
// 0. Спільне сховище ключів DataProtection
// =======================================================
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/home/vagrant/Edo-Sign3/shared-keys"))
    .SetApplicationName("EdoSign")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

// =======================================================
// 1. Database (SQLite)
// =======================================================
builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=app.db"));

// =======================================================
// 2. ASP.NET Identity (локальні акаунти)
// =======================================================
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(opt =>
    {
        opt.Password.RequiredLength = 8;
        opt.Password.RequireDigit = true;
        opt.Password.RequireNonAlphanumeric = true;
        opt.Password.RequireUppercase = true;
        opt.Password.RequireLowercase = false;
        opt.Password.RequiredUniqueChars = 1;
        opt.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// =======================================================
// 3. Authentication (SSO через EdoAuthServer)
// =======================================================
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, o =>
{
    o.Cookie.SameSite = SameSiteMode.None;           // 🔸 змінюємо на None
    o.Cookie.SecurePolicy = CookieSecurePolicy.None; // 🔸 HTTP дозволено
})

    .AddOpenIdConnect("oidc", options =>
{
    options.Authority = "http://localhost:7090"; // EdoAuthServer
    options.RequireHttpsMetadata = false;
    options.ClientId = "mvc";
    options.ClientSecret = "secret";
    options.ResponseType = "code";
    options.SaveTokens = true;

    // 🔹 імітаційна обробка токенів (не перевіряємо підпис)
    options.TokenValidationParameters.ValidateIssuer = false;
    options.TokenValidationParameters.ValidateAudience = false;
    options.TokenValidationParameters.SignatureValidator = (token, _) => new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(token);

    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");

    options.GetClaimsFromUserInfoEndpoint = false;
});


