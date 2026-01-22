using cfm_frontend.Extensions;
using cfm_frontend.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace cfm_frontend.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            // Check if user is authenticated (has valid Remember Me cookie)
            if (User.Identity?.IsAuthenticated == true)
            {
                var userInfo = HttpContext.Session.GetUserInfo();
                ViewBag.IsAuthenticated = true;
                ViewBag.UserName = userInfo?.FullName ?? User.Identity.Name ?? "User";
            }
            else
            {
                ViewBag.IsAuthenticated = false;
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
