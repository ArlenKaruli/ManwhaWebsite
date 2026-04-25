using ManwhaWebsite.Models;
using ManwhaWebsite.Models.ManhwaVault.Services;
using Microsoft.AspNetCore.Mvc;

namespace ManwhaWebsite.Controllers
{
    [Route("search")]
    public class SearchController : Controller
    {
        private readonly AniListService _aniList;

        public SearchController(AniListService aniList) => _aniList = aniList;

        [HttpGet("")]
        public async Task<IActionResult> Results(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Redirect("/");
            var results = await _aniList.SearchAsync(q.Trim(), 20);
            ViewData["Title"] = $"Search: {q}";
            ViewData["Query"] = q.Trim();
            return View("Results", results);
        }

        [HttpGet("suggest")]
        public async Task<IActionResult> Suggest(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return Json(Array.Empty<object>());
            var results = await _aniList.SearchAsync(q.Trim(), 8);
            return Json(results.Select(m => new { id = m.Id, title = m.Title, cover = m.CoverImageUrl, score = m.Rating }));
        }
    }
}
