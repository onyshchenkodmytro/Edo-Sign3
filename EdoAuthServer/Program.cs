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
// 0. Загальні налаштування
// =======================================================
builder.WebHost.UseUrls("http://0.0.0.0:7090");

// =======================================================
// 1. Data Protection (спільне сховище ключів між проєктами)
// =======================================================
var sharedKeysPath = "/home/vagrant/Edo-Sign3/shared-keys";
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(sharedKeysPath))
    .SetApplicationName("EdoSign.Shared")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

// 🔹 Примусова перевірка створення ключів
var dpProvider = DataProtectionProvider.Create(new DirectoryInfo(sharedKeysPath));
var protector = dpProvider.CreateProtector("StartupTest");
var testValue = protector.Protect("hello");
Console.WriteLine($"🔐 DataProtection test OK, sample: {testValue.Substring(0, 10)}...");

// =======================================================
// 2. Політика для cookie
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
app.UseCookiePolicy();
app.UseIdentityServer();
app.UseAuthorization();

app.MapRazorPages();
app.MapDefaultControllerRoute();

app.Run();

