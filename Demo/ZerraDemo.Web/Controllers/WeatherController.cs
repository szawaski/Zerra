using Microsoft.AspNetCore.Mvc;

namespace ZerraDemo.Web.Controllers
{
    public class WeatherController : Controller
    {
        public IActionResult Index() { return View(); }
    }
}
