using System.Diagnostics;
using ManwhaWebsite.Models;
using ManwhaWebsite.Models.ManhwaVault.Services;
using Microsoft.AspNetCore.Mvc;

namespace ManwhaWebsite.Controllers
{
    public class HomeController : Controller
    {
        private readonly AniListService _aniList;

        public HomeController(AniListService aniList)
        {
            _aniList = aniList;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _aniList.GetHomepageDataAsync();
            return View(data);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
