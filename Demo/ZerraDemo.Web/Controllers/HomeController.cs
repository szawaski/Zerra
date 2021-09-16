using Microsoft.AspNetCore.Mvc;

namespace ZerraDemo.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() { return View(); }
    }
}
