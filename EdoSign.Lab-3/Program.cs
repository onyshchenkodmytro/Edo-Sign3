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
// 2. ASP.NET Identity (локальні акаунти)
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
builder.Services.AddAuthentication(options =>
{
    // cookie-схема використовується для локальної автентифікації
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;

    // коли користувач неавторизований — система викликає SSO
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
.AddOpenIdConnect("oidc", options =>
{
    // === Основні параметри OpenID Connect ===
    options.Authority = "https://localhost:7090"; // URL EdoAuthServer
    options.ClientId = "mvc";
    options.ClientSecret = "secret";
    options.ResponseType = "code";

    // === Дозволені області (мають збігатися з Config.cs на сервері) ===
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.Scope.Add("custom_profile");
    options.Scope.Add("edolab.api");

    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;

    // ?? Задаємо, які поля використовувати як ім’я користувача / роль
    options.TokenValidationParameters.NameClaimType = "preferred_username";
    options.TokenValidationParameters.RoleClaimType = "role";

    // ?? Під час розробки дозволяємо самопідписаний сертифікат
    options.RequireHttpsMetadata = false;
});

// =======================================================
// 4. MVC + Views
builder.Services.AddControllersWithViews();

// =======================================================
// 5. Authorization (усі сторінки захищені за потреби)
builder.Services.AddAuthorization();

// =======================================================
// 6. Dependency Injection
builder.Services.AddSingleton<ISigner, RsaSigner>();
builder.Services.AddScoped<CryptoService>();

// =======================================================
// 7. Build app
var app = builder.Build();

// =======================================================
// 8. DB auto-migration (створення / оновлення БД)
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

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();   // ?? спочатку автентифікація
app.UseAuthorization();    // ?? потім авторизація

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

