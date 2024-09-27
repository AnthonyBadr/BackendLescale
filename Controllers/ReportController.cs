using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    public class ReportController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
