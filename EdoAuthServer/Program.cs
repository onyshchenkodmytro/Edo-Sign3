using Duende.IdentityServer;
using EdoAuthServer.Data;
using EdoAuthServer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// =======================================================
// 0. Загальні налаштування
// =======================================================
builder.WebHost.UseUrls("http://0.0.0.0:7090");

// =======================================================
// 1. Data Protection (спільне сховище ключів між проєктами)
// =======================================================
var sharedKeysPath = "/home/vagrant/Edo-Sign3/shared-keys";
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(sharedKeysPath))
    .SetApplicationName("EdoSign")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

// =======================================================
// 2. Cookie політика (для HTTP, без Secure, SameSite=Lax)
// =======================================================
builder.Services.ConfigureApplicationCookie(o =>
{
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = CookieSecurePolicy.None;
});
builder.Services.AddIdentityServer(options =>
{
    options.Authentication.CookieSameSiteMode = SameSiteMode.Lax;
});

builder.Services.PostConfigure<CookieAuthenticationOptions>(IdentityServerConstants.DefaultCookieAuthenticationScheme, options =>
{
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
});

builder.Services.PostConfigure<CookieAuthenticationOptions>(IdentityServerConstants.ExternalCookieAuthenticationScheme, options =>
{
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
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
// 5. IdentityServer (Duende)
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
    .AddDeveloperSigningCredential(persistKey: true,
        fileName: Path.Combine(sharedKeysPath, "tempkey.rsa")); // 🔹 підпис у спільному каталозі

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
app.UseCookiePolicy();
app.UseIdentityServer();
app.UseAuthorization();

app.MapRazorPages();
app.MapDefaultControllerRoute();

app.Run();
