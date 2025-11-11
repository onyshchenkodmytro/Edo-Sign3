using EdoSign.Lab_3.Data;
using EdoSign.Lab_3.Models;
using EdoSign.Signing;
using EdoSign.Lab_3.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

// =======================================================
// 1. Database (SQLite)
builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=app.db"));

// =======================================================
// 2. ASP.NET Identity (ëîêàëüí³ àêàóíòè)
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
// 3. Authentication (SSO ÷åðåç EdoAuthServer)
builder.Services.AddAuthentication(options =>
{
    // cookie-ñõåìà âèêîðèñòîâóºòüñÿ äëÿ ëîêàëüíî¿ àâòåíòèô³êàö³¿
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;

    // êîëè êîðèñòóâà÷ íåàâòîðèçîâàíèé — ñèñòåìà âèêëèêàº SSO
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
.AddOpenIdConnect("oidc", options =>
{
    // === Îñíîâí³ ïàðàìåòðè OpenID Connect ===
    options.Authority = "http://localhost:7090"; // URL EdoAuthServer
    options.RequireHttpsMetadata = false; 
    options.ClientId = "mvc";
    options.ClientSecret = "secret";
    options.ResponseType = "code";

    // === Äîçâîëåí³ îáëàñò³ (ìàþòü çá³ãàòèñÿ ç Config.cs íà ñåðâåð³) ===
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.Scope.Add("custom_profile");
    options.Scope.Add("edolab.api");

    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;

    // ?? Çàäàºìî, ÿê³ ïîëÿ âèêîðèñòîâóâàòè ÿê ³ì’ÿ êîðèñòóâà÷à / ðîëü
    options.TokenValidationParameters.NameClaimType = "preferred_username";
    options.TokenValidationParameters.RoleClaimType = "role";

    // ?? Ï³ä ÷àñ ðîçðîáêè äîçâîëÿºìî ñàìîï³äïèñàíèé ñåðòèô³êàò
    options.RequireHttpsMetadata = false;
});

// =======================================================
// 4. MVC + Views
builder.Services.AddControllersWithViews();

// =======================================================
// 5. Authorization (óñ³ ñòîð³íêè çàõèùåí³ çà ïîòðåáè)
builder.Services.AddAuthorization();

// =======================================================
// 6. Dependency Injection
builder.Services.AddSingleton<ISigner, RsaSigner>();
builder.Services.AddScoped<CryptoService>();

// =======================================================
// 7. Build app
var app = builder.Build();

// =======================================================
// 8. DB auto-migration (ñòâîðåííÿ / îíîâëåííÿ ÁÄ)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// =======================================================
// 9. Middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();   // ?? ñïî÷àòêó àâòåíòèô³êàö³ÿ
app.UseAuthorization();    // ?? ïîò³ì àâòîðèçàö³ÿ

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

