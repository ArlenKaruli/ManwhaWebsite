using ManwhaWebsite.Models;
using ManwhaWebsite.Models.ManhwaVault.Services;
using Microsoft.AspNetCore.Mvc;

namespace ManwhaWebsite.Controllers
{
    public class RankingsController : Controller
    {
        private readonly AniListService _aniList;

        public RankingsController(AniListService aniList)
        {
            _aniList = aniList;
        }

        public async Task<IActionResult> Index()
        {
            var vm = await _aniList.GetRankingsAsync();
            ViewData["Title"] = "Rankings";
            return View(vm);
        }
    }
}
