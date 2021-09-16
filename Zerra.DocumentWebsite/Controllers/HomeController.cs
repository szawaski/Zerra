using Microsoft.AspNetCore.Mvc;

namespace Zerra.DocumentWebsite.Controllers
{
    [Route("")]
    public class HomeController : Controller
    {
        [Route("")]
        public IActionResult Index() { return View(); }
        [Route("[Action]")]
        public IActionResult Core() { return View(); }
        [Route("[Action]")]
        public IActionResult CQRS() { return View(); }
        [Route("[Action]")]
        public IActionResult Repository() { return View(); }
    }
}
