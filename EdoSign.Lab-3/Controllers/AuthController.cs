using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EdoSign.Lab_3.Controllers;

public class AuthController : Controller
{
    [HttpPost, AllowAnonymous]
    public IActionResult Login(string? returnUrl = "/")
    {
        // запустить OIDC challenge
        return Challenge(new AuthenticationProperties { RedirectUri = returnUrl }, "oidc");
    }

    [HttpPost]
    public IActionResult Logout()
    {
        return SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            CookieAuthenticationDefaults.AuthenticationScheme,
            "oidc");
    }
}
