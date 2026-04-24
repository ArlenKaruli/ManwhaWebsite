using ManwhaWebsite.Data;
using ManwhaWebsite.Models;
using ManwhaWebsite.Models.ManhwaVault.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ManwhaWebsite.Controllers
{
    [Route("manga")]
    public class MangaController : Controller
    {
        private readonly AniListService _aniList;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MangaController(AniListService aniList, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _aniList = aniList;
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var vm = await _aniList.GetMangaDetailAsync(id);
            if (vm == null) return NotFound();

            vm.Reviews = await _context.UserManhwaReviews
                .Where(r => r.AniListId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = _userManager.GetUserId(User);
                var myRating = await _context.UserManhwaRatings
                    .FirstOrDefaultAsync(r => r.UserId == userId && r.AniListId == id);
                vm.CurrentUserRating = myRating?.Score;
            }

            return View(vm);
        }

        [HttpPost("{id:int}/rate")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rate(int id, int score)
        {
            score = Math.Clamp(score, 1, 10);
            var userId = _userManager.GetUserId(User)!;

            var existing = await _context.UserManhwaRatings
                .FirstOrDefaultAsync(r => r.UserId == userId && r.AniListId == id);

            if (existing == null)
            {
                _context.UserManhwaRatings.Add(new UserManhwaRating
                {
                    UserId = userId,
                    AniListId = id,
                    Score = score,
                    CreatedAt = DateTime.UtcNow,
                });
            }
            else
            {
                existing.Score = score;
                existing.CreatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost("{id:int}/review")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(int id, int score, string comment)
        {
            if (string.IsNullOrWhiteSpace(comment) || comment.Trim().Length < 10)
                return RedirectToAction(nameof(Details), new { id });

            score = Math.Clamp(score, 1, 10);
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Details), new { id });

            var displayName = string.IsNullOrWhiteSpace(user.DisplayName) ? (user.Email ?? "User") : user.DisplayName;

            var existing = await _context.UserManhwaReviews
                .FirstOrDefaultAsync(r => r.UserId == user.Id && r.AniListId == id);

            if (existing == null)
            {
                _context.UserManhwaReviews.Add(new UserManhwaReview
                {
                    UserId = user.Id,
                    UserDisplayName = displayName,
                    UserProfilePicture = user.ProfilePictureUrl,
                    AniListId = id,
                    Score = score,
                    Comment = comment.Trim(),
                    CreatedAt = DateTime.UtcNow,
                });
            }
            else
            {
                existing.Score = score;
                existing.Comment = comment.Trim();
                existing.CreatedAt = DateTime.UtcNow;
                existing.UserDisplayName = displayName;
                existing.UserProfilePicture = user.ProfilePictureUrl;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
