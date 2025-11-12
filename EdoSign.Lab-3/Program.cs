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

// =======================================================
// 0️⃣ ВАЖЛИВО: дозволяємо SameSite=None без HTTPS
// =======================================================
AppContext.SetSwitch("Microsoft.AspNetCore.Authentication.SuppressSameSiteNone", true);

// =======================================================
// 1️⃣ Спільне сховище ключів DataProtection
// =======================================================
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/home/vagrant/Edo-Sign3/shared-keys"))
    .SetApplicationName("EdoSign")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

// =======================================================
// 2️⃣ База даних (SQLite)
// =======================================================
builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=app.db"));

// =======================================================
// 3️⃣ Identity (локальні акаунти)
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
// 4️⃣ Authentication (через EdoAuthServer)
// =======================================================
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = "oidc";
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.Cookie.SameSite = SameSiteMode.Lax;             // ✅ безпечний режим для HTTP
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;  // ✅ дозволяємо без HTTPS
})
.AddOpenIdConnect("oidc", options =>
{
    options.Authority = "http://localhost:7090";   // твій EdoAuthServer
    options.RequireHttpsMetadata = false;           // без HTTPS
    options.ClientId = "mvc";
    options.ClientSecret = "secret";
    options.ResponseType = "code";
    options.SaveTokens = true;

    options.Scope.Add("openid");
    options.Scope.Add("profile");

    // ✅ "костиль" — не перевіряємо підпис токенів (для демо)
    options.TokenValidationParameters.ValidateIssuer = false;
    options.TokenValidationParameters.ValidateAudience = false;
    options.TokenValidationParameters.SignatureValidator = (token, _) =>
    {
        return new JwtSecurityToken(token);
    };

    // ✅ не тягнемо claims через UserInfo endpoint (щоб не ламалось)
    options.GetClaimsFromUserInfoEndpoint = false;

    // ✅ Кукі для HTTP режиму
    options.CorrelationCookie.SameSite = SameSiteMode.Lax;
    options.NonceCookie.SameSite = SameSiteMode.Lax;
});

// =======================================================
// 5️⃣ MVC + Views
// =======================================================
builder.Services.AddControllersWithViews();

// =======================================================
// 6️⃣ Authorization
// =======================================================
builder.Services.AddAuthorization();

// =======================================================
// 7️⃣ Dependency Injection
// =======================================================
builder.Services.AddSingleton<ISigner, RsaSigner>();
builder.Services.AddScoped<CryptoService>();

// =======================================================
// 8️⃣ Build app
// =======================================================
var app = builder.Build();

// =======================================================
// 9️⃣ Автоматичне оновлення БД
// =======================================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// =======================================================
// 🔟 Middleware pipeline
// =======================================================
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// =======================================================
// 11️⃣ Routing
// =======================================================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// =======================================================
// 🚀 Запуск
// =======================================================
app.Run();
