using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using EdoAuthServer.Data;
using EdoAuthServer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// =======================================================
// 0. Загальні налаштування сервера
// =======================================================
builder.WebHost.UseUrls("http://0.0.0.0:7090");

// =======================================================
// 1. Сховище ключів DataProtection
// =======================================================
// Це важливо, щоб уникнути помилок типу “key not found in key ring”
// і щоб підписані cookie / токени не втрачались після рестарту.
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/home/vagrant/Edo-Sign3/shared-keys"))
    .SetApplicationName("EdoSign.Shared")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

// =======================================================
// 2. Політика для cookie (виправлення SameSite помилок)
// =======================================================
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
    options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.None;
    options.Secure = Microsoft.AspNetCore.Http.CookieSecurePolicy.None;
});

// =======================================================
// 3. База даних
// =======================================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// =======================================================
// 4. ASP.NET Identity
// =======================================================
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// =======================================================
// 5. IdentityServer (Duende) + інтеграція з Identity
// =======================================================
builder.Services
    .AddIdentityServer(options =>
    {
        options.Events.RaiseErrorEvents = true;
        options.Events.RaiseInformationEvents = true;
        options.Events.RaiseFailureEvents = true;
        options.Events.RaiseSuccessEvents = true;
    })
    .AddAspNetIdentity<ApplicationUser>()
    .AddProfileService<EdoAuthServer.Services.ProfileService>()
    .AddInMemoryIdentityResources(Config.IdentityResources)
    .AddInMemoryApiScopes(Config.ApiScopes)
    .AddInMemoryClients(Config.Clients)
    // Тимчасовий сертифікат для dev-середовища (HTTP)
    .AddDeveloperSigningCredential(persistKey: true);

// =======================================================
// 6. MVC + Razor Pages
// =======================================================
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages()
    .WithRazorPagesRoot("/EdoAuthServer.UI/Pages");

// =======================================================
// 7. Build
// =======================================================
var app = builder.Build();

// =======================================================
// 8. Middleware pipeline
// =======================================================
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseRouting();
app.UseCookiePolicy();          // важливо: до IdentityServer
app.UseIdentityServer();
app.UseAuthorization();

app.MapRazorPages();
app.MapDefaultControllerRoute();

app.Run();

