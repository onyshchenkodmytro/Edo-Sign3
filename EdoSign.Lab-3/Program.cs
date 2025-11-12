using EdoSign.Lab_3.Data;
using EdoSign.Lab_3.Models;
using EdoSign.Signing;
using EdoSign.Lab_3.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// =======================================================
// 1️⃣ База даних (SQLite)
// =======================================================
builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=app.db"));

// =======================================================
// 2️⃣ Identity (локальні акаунти)
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
// 3️⃣ Authentication (Cookie-only, без зовнішнього SSO-сервера)
// =======================================================
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/SSO/LoginDemo";  // коли користувач неавторизований
    options.LogoutPath = "/SSO/Logout";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
});

// =======================================================
// 4️⃣ MVC + Views
// =======================================================
builder.Services.AddControllersWithViews();

// =======================================================
// 5️⃣ Dependency Injection
// =======================================================
builder.Services.AddSingleton<ISigner, RsaSigner>();
builder.Services.AddScoped<CryptoService>();

var app = builder.Build();

// =======================================================
// 6️⃣ Міграції БД
// =======================================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// =======================================================
// 7️⃣ Middleware
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
// 8️⃣ Routing
// =======================================================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
