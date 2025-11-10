// Copyright (c) Duende Software. 
// All rights reserved. See LICENSE in the project root for license information.

using Duende.IdentityServer;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EdoAuthServer.UI.Pages.Home;

[AllowAnonymous]
public class Index : PageModel
{
    // Видаляємо залежність від IdentityServerLicense, якої немає у дев-версії
    // public Index(IdentityServerLicense? license = null)
    // {
    //     License = license;
    // }

    // Проста ініціалізація без ліцензійної інформації
    public Index()
    {
    }

    public string Version
    {
        get => typeof(Duende.IdentityServer.Hosting.IdentityServerMiddleware).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion.Split('+').First()
            ?? "unavailable";
    }

    // Видаляємо або коментуємо це поле, бо клас IdentityServerLicense відсутній
    // public IdentityServerLicense? License { get; }
}

