// Copyright (c) Duende Software. All rights reserved.
// Modified for ASP.NET Identity integration by EdoSign project.

using Duende.IdentityServer;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using EdoAuthServer.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EdoAuthServer.UI.Pages.Login;

[SecurityHeaders]
[AllowAnonymous]
public class Index : PageModel
{
    private readonly IIdentityServerInteractionService _interaction;
    private readonly IEventService _events;
    private readonly IAuthenticationSchemeProvider _schemeProvider;
    private readonly IIdentityProviderStore _identityProviderStore;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public ViewModel View { get; set; } = default!;

    [BindProperty]
    public InputModel Input { get; set; } = default!;

    public Index(
        IIdentityServerInteractionService interaction,
        IAuthenticationSchemeProvider schemeProvider,
        IIdentityProviderStore identityProviderStore,
        IEventService events,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _interaction = interaction;
        _schemeProvider = schemeProvider;
        _identityProviderStore = identityProviderStore;
        _events = events;
        _signInManager = signInManager;
        _userManager = userManager;
    }

    public async Task<IActionResult> OnGet(string? returnUrl)
    {
        await BuildModelAsync(returnUrl);

        if (View.IsExternalLoginOnly)
        {
            // only one external provider
            return RedirectToPage("/ExternalLogin/Challenge", new { scheme = View.ExternalLoginScheme, returnUrl });
        }

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var context = await _interaction.GetAuthorizationContextAsync(Input.ReturnUrl);

        // If user clicked cancel
        if (Input.Button != "login")
        {
            if (context != null)
            {
                await _interaction.DenyAuthorizationAsync(context, AuthorizationError.AccessDenied);
                if (context.IsNativeClient())
                    return this.LoadingPage(Input.ReturnUrl);

                return Redirect(Input.ReturnUrl ?? "~/");
            }

            return Redirect("~/");
        }

        if (ModelState.IsValid)
        {
            // find user in DB
            var user = await _userManager.FindByNameAsync(Input.Username);
            if (user != null)
            {
                var result = await _signInManager.CheckPasswordSignInAsync(user, Input.Password, false);
                if (result.Succeeded)
                {
                    await _events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.UserName, clientId: context?.Client.ClientId));

                    var props = new AuthenticationProperties();
                    if (LoginOptions.AllowRememberLogin && Input.RememberLogin)
                    {
                        props.IsPersistent = true;
                        props.ExpiresUtc = DateTimeOffset.UtcNow.Add(LoginOptions.RememberMeLoginDuration);
                    }

                    var isuser = new IdentityServerUser(user.Id)
                    {
                        DisplayName = user.UserName
                    };

                    await HttpContext.SignInAsync(isuser, props);

                    if (context != null)
                    {
                        if (context.IsNativeClient())
                            return this.LoadingPage(Input.ReturnUrl);

                        return Redirect(Input.ReturnUrl ?? "~/");
                    }

                    if (Url.IsLocalUrl(Input.ReturnUrl))
                        return Redirect(Input.ReturnUrl);
                    else if (string.IsNullOrEmpty(Input.ReturnUrl))
                        return Redirect("~/");
                    else
                        throw new ArgumentException("Invalid return URL");
                }
            }

            const string error = "invalid credentials";
            await _events.RaiseAsync(new UserLoginFailureEvent(Input.Username, error, clientId: context?.Client.ClientId));
            ModelState.AddModelError(string.Empty, LoginOptions.InvalidCredentialsErrorMessage);
        }

        await BuildModelAsync(Input.ReturnUrl);
        return Page();
    }

    private async Task BuildModelAsync(string? returnUrl)
    {
        Input = new InputModel { ReturnUrl = returnUrl };

        var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
        if (context?.IdP != null && await _schemeProvider.GetSchemeAsync(context.IdP) != null)
        {
            var local = context.IdP == IdentityServerConstants.LocalIdentityProvider;
            View = new ViewModel
            {
                EnableLocalLogin = local
            };

            Input.Username = context.LoginHint;

            if (!local)
            {
                View.ExternalProviders = new[]
                {
                    new ViewModel.ExternalProvider(authenticationScheme: context.IdP)
                };
            }

            return;
        }

        var schemes = await _schemeProvider.GetAllSchemesAsync();
        var providers = schemes
            .Where(x => x.DisplayName != null)
            .Select(x => new ViewModel.ExternalProvider(
                authenticationScheme: x.Name,
                displayName: x.DisplayName ?? x.Name))
            .ToList();

        var dynamicSchemes = (await _identityProviderStore.GetAllSchemeNamesAsync())
            .Where(x => x.Enabled)
            .Select(x => new ViewModel.ExternalProvider(
                authenticationScheme: x.Scheme,
                displayName: x.DisplayName ?? x.Scheme));

        providers.AddRange(dynamicSchemes);

        var allowLocal = true;
        var client = context?.Client;
        if (client != null)
        {
            allowLocal = client.EnableLocalLogin;
            if (client.IdentityProviderRestrictions?.Count > 0)
            {
                providers = providers
                    .Where(p => client.IdentityProviderRestrictions.Contains(p.AuthenticationScheme))
                    .ToList();
            }
        }

        View = new ViewModel
        {
            AllowRememberLogin = LoginOptions.AllowRememberLogin,
            EnableLocalLogin = allowLocal && LoginOptions.AllowLocalLogin,
            ExternalProviders = providers.ToArray()
        };
    }
}
