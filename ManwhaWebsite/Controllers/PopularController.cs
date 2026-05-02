using ManwhaWebsite.Models;
using ManwhaWebsite.Models.ManhwaVault.Services;
using Microsoft.AspNetCore.Mvc;

namespace ManwhaWebsite.Controllers
{
    public class PopularController : Controller
    {
        private readonly AniListService _aniList;

        public PopularController(AniListService aniList) => _aniList = aniList;

        public async Task<IActionResult> Index()
        {
            var items = await _aniList.GetPopularAsync(1);
            ViewData["Title"] = "Popular Manhwa";
            return View(items);
        }

        public async Task<IActionResult> LoadMore(int page = 2)
        {
            var items = await _aniList.GetPopularAsync(page);
            return Json(items.Select(m => new
            {
                id = m.Id,
                title = m.Title,
                cover = m.CoverImageUrl,
                score = m.Rating,
                status = m.Status,
                chapter = m.LatestChapter,
                popularity = m.Popularity
            }));
        }
    }
}
