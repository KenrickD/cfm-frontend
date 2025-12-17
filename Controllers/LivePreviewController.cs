using Microsoft.AspNetCore.Mvc;

namespace Mvc.Controllers
{
    public class LivePreviewController : Controller
    {
        // GET: LivePreview
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult LivePreviewTwo()
        {
            return View();
        }
        public IActionResult LivePreviewTwoOld()
        {
            return View();
        }
    }
}