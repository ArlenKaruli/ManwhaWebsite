using ManwhaWebsite.Models;
using ManwhaWebsite.Models.ManhwaVault.Services;
using Microsoft.AspNetCore.Mvc;

namespace ManwhaWebsite.Controllers
{
    public class BrowseController : Controller
    {
        private readonly AniListService _aniList;

        public BrowseController(AniListService aniList) => _aniList = aniList;

        public async Task<IActionResult> Index()
        {
            var initial = await _aniList.GetDiscoverAsync();
            ViewData["Title"] = "Browse";
            return View("Index", initial);
        }

        public async Task<IActionResult> Filter(
            string? search,
            [FromQuery] List<string> genres,
            string? status,
            double? minRating,
            int? minChapters,
            int? maxChapters,
            int? publishedBefore,
            int? publishedAfter,
            int page = 1)
        {
            var results = await _aniList.BrowseAsync(
                search, genres, status, minRating,
                minChapters, maxChapters, publishedBefore, publishedAfter, page);

            return Json(results.Select(m => new
            {
                id = m.Id,
                title = m.Title,
                cover = m.CoverImageUrl,
                score = m.Rating,
                status = m.Status,
                chapters = m.ChapterCount,
                genres = m.Genres
            }));
        }
    }
}
