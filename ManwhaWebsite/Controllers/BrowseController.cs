using ManwhaWebsite.Models;
using ManwhaWebsite.Models.ManhwaVault.Services;
using ManwhaWebsite.Services;
using Microsoft.AspNetCore.Mvc;

namespace ManwhaWebsite.Controllers
{
    public class BrowseController : Controller
    {
        private readonly AniListService _aniList;
        private readonly MangaUpdatesService _mangaUpdates;

        public BrowseController(AniListService aniList, MangaUpdatesService mangaUpdates)
        {
            _aniList = aniList;
            _mangaUpdates = mangaUpdates;
        }

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

            bool isReleasing = string.Equals(status, "RELEASING", StringComparison.OrdinalIgnoreCase);
            bool needsChapterFilter = isReleasing && (minChapters > 0 || maxChapters > 0);

            if (needsChapterFilter)
            {
                // Fetch chapter counts from MangaUpdates in parallel (max 5 concurrent).
                var semaphore = new SemaphoreSlim(5);
                var chapterCounts = await Task.WhenAll(results.Select(async m =>
                {
                    await semaphore.WaitAsync();
                    try { return (m.Id, Count: await _mangaUpdates.GetTotalChaptersByTitleAsync(m.Title)); }
                    finally { semaphore.Release(); }
                }));

                var lookup = chapterCounts.ToDictionary(x => x.Id, x => x.Count);

                results = results.Where(m =>
                {
                    var count = lookup.TryGetValue(m.Id, out var c) ? c : m.ChapterCount;
                    if (count == null) return true; // can't determine — keep it
                    if (minChapters > 0 && count < minChapters) return false;
                    if (maxChapters > 0 && count > maxChapters) return false;
                    return true;
                }).ToList();
            }

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
