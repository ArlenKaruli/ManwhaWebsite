using System.Diagnostics;
using ManwhaWebsite.Models;
using ManwhaWebsite.Models.ManhwaVault.Services;
using ManwhaWebsite.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ManwhaWebsite.Controllers
{
    public class HomeController : Controller
    {
        private readonly AniListService _aniList;
        private readonly RecommendationService _recommendations;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(AniListService aniList, RecommendationService recommendations, UserManager<ApplicationUser> userManager)
        {
            _aniList = aniList;
            _recommendations = recommendations;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            HomeViewModel data;
            try
            {
                data = await _aniList.GetHomepageDataAsync();
            }
            catch (Exception)
            {
                Response.StatusCode = 503;
                return View("ServiceDown");
            }

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = _userManager.GetUserId(User)!;
                data.Recommendations = await _recommendations.GetRecommendationsAsync(userId);
            }

            return View(data);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> RandomDiscover()
        {
            try
            {
                var items = await _aniList.GetDiscoverAsync();
                return Json(items.Select(m => new
                {
                    id = m.Id,
                    title = m.Title,
                    description = m.Description,
                    cover = m.CoverImageUrl,
                    score = m.Rating,
                    popularity = m.Popularity,
                }));
            }
            catch (Exception)
            {
                return StatusCode(503, new { error = "Service temporarily unavailable." });
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
