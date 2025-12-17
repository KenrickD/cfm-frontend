using Microsoft.AspNetCore.Mvc;

namespace cfm_frontend.Controllers
{
    public class LoginController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        
    }
}
