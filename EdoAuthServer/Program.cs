using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using EdoAuthServer.Data;
using EdoAuthServer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:7090");

// =======================================================
// 1. Підключення до спільної БД EdoSign.Lab-3
// =======================================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// =======================================================
// 2. ASP.NET Identity — ті ж користувачі, що й у головному проєкті
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
// 3. IdentityServer (Duende) + інтеграція з Identity
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
    .AddDeveloperSigningCredential(); // тимчасовий сертифікат

// =======================================================
// 4. MVC + Razor Pages
// =======================================================
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages()
    .WithRazorPagesRoot("/EdoAuthServer.UI/Pages");

// =======================================================
// 5. Build app
// =======================================================
var app = builder.Build();

// =======================================================
// 6. Middleware pipeline
// =======================================================
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseRouting();

app.UseIdentityServer();
app.UseAuthorization();

app.MapRazorPages();
app.MapDefaultControllerRoute();

app.Run();
