using ManwhaWebsite.Models;
using ManwhaWebsite.Models.ManhwaVault.Services;
using Microsoft.AspNetCore.Mvc;

namespace ManwhaWebsite.Controllers
{
    public class LatestController : Controller
    {
        private readonly AniListService _aniList;

        public LatestController(AniListService aniList) => _aniList = aniList;

        public async Task<IActionResult> Index()
        {
            var items = await _aniList.GetLatestAsync(1);
            ViewData["Title"] = "Latest Updates";
            return View(items);
        }

        public async Task<IActionResult> LoadMore(int page = 2)
        {
            var items = await _aniList.GetLatestAsync(page);
            return Json(items.Select(m => new
            {
                id = m.Id,
                title = m.Title,
                cover = m.CoverImageUrl,
                score = m.Rating,
                status = m.Status,
                chapter = m.LatestChapter,
                updatedAt = new DateTimeOffset(m.LastUpdated, TimeSpan.Zero).ToUnixTimeSeconds()
            }));
        }
    }
}
