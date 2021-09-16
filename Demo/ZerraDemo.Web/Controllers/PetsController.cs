using Microsoft.AspNetCore.Mvc;

namespace ZerraDemo.Web.Controllers
{
    public class PetsController : Controller
    {
        public IActionResult Index() { return View(); }
        public IActionResult Adopt() { return View(); }
    }
}
