using Microsoft.AspNetCore.Mvc;

namespace ZerraDemo.Web.Controllers
{
    public class WeatherCachedController : Controller
    {
        public IActionResult Index() { return View(); }
    }
}
