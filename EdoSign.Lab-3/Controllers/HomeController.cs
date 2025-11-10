using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EdoSign.Lab_3.Controllers;

public class HomeController : Controller
{
    [AllowAnonymous]
    public IActionResult Index() => View();

}



