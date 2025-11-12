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
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

// 0) Дозволити SameSite=None без HTTPS (для локального демо)
AppContext.SetSwitch("Microsoft.AspNetCore.Authentication.SuppressSameSiteNone", true);

// 1) Спільні ключі DataProtection (щоб після рестарту кукі не ламались)
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/home/vagrant/Edo-Sign3/shared-keys"))
    .SetApplicationName("EdoSign")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

// 2) База (SQLite)
builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=app.db"));

// 3) Identity
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

// 4) Authentication
// ГОЛОВНЕ: основна схема = Cookies (для FakeSSO і SignInManager).
// OIDC залишаємо для вигляду/демо, але він НЕ заважає локальному входу.
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme; // "Cookies"
    // Не ставимо DefaultChallengeScheme = "oidc", щоб кнопка фейкового входу не кидала на OIDC
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/FakeSSO/LoginDemo";        // куди перекидати, якщо неавторизований
    options.AccessDeniedPath = "/Home/AccessDenied"; // опціонально
    options.Cookie.SameSite = SameSiteMode.Lax;      // працює по HTTP
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
})
.AddOpenIdConnect("oidc", options =>
{
    // Лишається як “друга” схема (опційна). Можеш показати, що в проекті є SSO.
    options.Authority = "http://localhost:7090";
    options.RequireHttpsMetadata = false;
    options.ClientId = "mvc";
    options.ClientSecret = "secret";
    options.ResponseType = "code";
    options.SaveTokens = true;

    options.Scope.Add("openid");
    options.Scope.Add("profile");

    // Демонстраційні послаблення (щоб не спіткнутись об підписи токена)
    options.TokenValidationParameters.ValidateIssuer = false;
    options.TokenValidationParameters.ValidateAudience = false;
    options.TokenValidationParameters.SignatureValidator = (token, _) =>
        new JwtSecurityToken(token);

    options.GetClaimsFromUserInfoEndpoint = false;

    options.CorrelationCookie.SameSite = SameSiteMode.Lax;
    options.NonceCookie.SameSite = SameSiteMode.Lax;
});

// 5) MVC
builder.Services.AddControllersWithViews();

// 6) Authorization
builder.Services.AddAuthorization();

// 7) DI
builder.Services.AddSingleton<ISigner, RsaSigner>();
builder.Services.AddScoped<CryptoService>();

var app = builder.Build();

// 8) Автоміграції
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// 9) Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// 10) Роути
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
