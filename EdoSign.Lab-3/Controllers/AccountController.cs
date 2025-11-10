using EdoSign.Lab_3.Models;
using EdoSign.Lab_3.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EdoSign.Lab_3.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly SignInManager<ApplicationUser> _signIn;

        public AccountController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn)
        {
            _users = users;
            _signIn = signIn;
        }

        // ================================
        // === REGISTER ===================
        // ================================
        [AllowAnonymous]
        public IActionResult Register() => View(new RegisterViewModel());

        [HttpPost, AllowAnonymous]
        public async Task<IActionResult> Register(RegisterViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            // Перевірка логіну
            var existingUser = await _users.FindByNameAsync(vm.UserName);
            if (existingUser != null)
            {
                ModelState.AddModelError(nameof(vm.UserName), "Користувач з таким логіном вже існує");
                return View(vm);
            }

            // Створення користувача
            var user = new ApplicationUser
            {
                UserName = vm.UserName,
                Email = vm.Email,
                PhoneNumber = vm.Phone,
                FullName = vm.FullName
            };

            var result = await _users.CreateAsync(user, vm.Password);
            if (result.Succeeded)
            {
                await _signIn.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Profile");
            }

            foreach (var e in result.Errors)
                ModelState.AddModelError(string.Empty, e.Description);

            return View(vm);
        }

        // ================================
        // === LOGIN =====================
        // ================================
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost, AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel vm, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(vm);

            // Знайти користувача по логіну
            var user = await _users.FindByNameAsync(vm.UserNameOrEmail);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Користувача не знайдено");
                return View(vm);
            }

            var result = await _signIn.PasswordSignInAsync(
                user.UserName,
                vm.Password,
                vm.RememberMe,
                lockoutOnFailure: false);

            if (result.Succeeded)
                return RedirectToLocal(returnUrl);

            if (result.IsLockedOut)
                ModelState.AddModelError("", "Акаунт заблокований");
            else
                ModelState.AddModelError("", "Невірний логін або пароль");

            return View(vm);
        }

        // ================================
        // === LOGOUT =====================
        // ================================
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signIn.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // ================================
        // === PROFILE ====================
        // ================================
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _users.GetUserAsync(User);
            return View(user);
        }

        // ================================
        // === EXTERNAL LOGIN (SSO / Google)
        // ================================
        [HttpPost, AllowAnonymous]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var props = _signIn.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(props, provider);
        }

        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            if (remoteError != null)
            {
                TempData["Error"] = $"Помилка зовнішнього входу: {remoteError}";
                return RedirectToAction(nameof(Login));
            }

            var info = await _signIn.GetExternalLoginInfoAsync();
            if (info == null)
                return RedirectToAction(nameof(Login));

            // 🟢 Отримуємо ім’я користувача з claims
            var userName = info.Principal.FindFirstValue(ClaimTypes.Name)
                           ?? info.Principal.FindFirstValue("preferred_username")
                           ?? info.Principal.Identity?.Name;

            if (string.IsNullOrEmpty(userName))
            {
                TempData["Error"] = "Зовнішній провайдер не повернув логін користувача.";
                return RedirectToAction(nameof(Login));
            }

            // 🟢 Шукаємо користувача по UserName (а не по email)
            var user = await _users.FindByNameAsync(userName);
            if (user != null)
            {
                await _signIn.SignInAsync(user, isPersistent: false);
                return RedirectToLocal(returnUrl);
            }

            // 🟡 Якщо не знайдено — не створюємо нового
            TempData["Error"] = $"Користувача '{userName}' не знайдено у локальній базі.";
            return RedirectToAction(nameof(Login));
        }

        // ================================
        // === HELPERS ====================
        // ================================
        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Profile", "Account");
        }
    }
}
