using Microsoft.AspNetCore.Mvc;

namespace ZerraDemo.Web.Controllers
{
    public class Ledger2Controller : Controller
    {
        public IActionResult Index() { return View(); }
        public IActionResult History() { return View(); }
        public IActionResult Transact() { return View(); }
    }
}
