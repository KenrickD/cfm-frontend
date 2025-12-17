using Microsoft.AspNetCore.Mvc;

namespace Mvc.Controllers
{
    public class ChartsController : Controller
    {
        // GET: Charts
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult ChartPeity()
        {
            return View();
        }
    }
}