using ManwhaWebsite.Data;
using ManwhaWebsite.Models;
using ManwhaWebsite.Models.ManhwaVault.Services;
using ManwhaWebsite.Services;
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
        private readonly MangaUpdatesService _mangaUpdates;

        public MangaController(AniListService aniList, ApplicationDbContext context, UserManager<ApplicationUser> userManager, MangaUpdatesService mangaUpdates)
        {
            _aniList = aniList;
            _context = context;
            _userManager = userManager;
            _mangaUpdates = mangaUpdates;
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var vm = await _aniList.GetMangaDetailAsync(id);
            if (vm == null) return NotFound();

            if (vm.Chapters == null)
                vm.Chapters = await _mangaUpdates.GetTotalChaptersByTitleAsync(vm.Title);

            vm.Reviews = await _context.UserManhwaReviews
                .Where(r => r.AniListId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var reviewIds = vm.Reviews.Select(r => r.Id).ToList();
            var allVotes = await _context.ReviewVotes
                .Where(v => reviewIds.Contains(v.ReviewId))
                .ToListAsync();

            vm.ReviewUpvotes   = allVotes.Where(v => v.IsUpvote).GroupBy(v => v.ReviewId)
                                         .ToDictionary(g => g.Key, g => g.Count());
            vm.ReviewDownvotes = allVotes.Where(v => !v.IsUpvote).GroupBy(v => v.ReviewId)
                                         .ToDictionary(g => g.Key, g => g.Count());

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = _userManager.GetUserId(User);
                var myRating = await _context.UserManhwaRatings
                    .FirstOrDefaultAsync(r => r.UserId == userId && r.AniListId == id);
                vm.CurrentUserRating = myRating?.Score;

                var listEntry = await _context.UserReadingLists
                    .FirstOrDefaultAsync(e => e.UserId == userId && e.AniListId == id);
                vm.CurrentUserReadingStatus = listEntry?.Status;

                var myVotes = allVotes.Where(v => v.VoterUserId == userId).ToList();
                vm.CurrentUserUpvotedIds   = myVotes.Where(v => v.IsUpvote).Select(v => v.ReviewId).ToHashSet();
                vm.CurrentUserDownvotedIds = myVotes.Where(v => !v.IsUpvote).Select(v => v.ReviewId).ToHashSet();
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

        [HttpPost("{id:int}/review/{reviewId:int}/vote")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Vote(int id, int reviewId, [FromForm] bool isUpvote)
        {
            var userId = _userManager.GetUserId(User)!;

            var review = await _context.UserManhwaReviews.FindAsync(reviewId);
            if (review == null || review.AniListId != id)
                return Json(new { success = false });

            // Users cannot vote on their own reviews
            if (review.UserId == userId)
                return Json(new { success = false });

            var existing = await _context.ReviewVotes
                .FirstOrDefaultAsync(v => v.ReviewId == reviewId && v.VoterUserId == userId);

            if (existing == null)
            {
                _context.ReviewVotes.Add(new ReviewVote
                {
                    ReviewId = reviewId,
                    VoterUserId = userId,
                    IsUpvote = isUpvote,
                    CreatedAt = DateTime.UtcNow,
                });
            }
            else if (existing.IsUpvote == isUpvote)
            {
                // Same vote again — toggle off
                _context.ReviewVotes.Remove(existing);
            }
            else
            {
                // Switching vote direction
                existing.IsUpvote = isUpvote;
            }

            await _context.SaveChangesAsync();

            var upvotes   = await _context.ReviewVotes.CountAsync(v => v.ReviewId == reviewId && v.IsUpvote);
            var downvotes = await _context.ReviewVotes.CountAsync(v => v.ReviewId == reviewId && !v.IsUpvote);
            var userVote  = await _context.ReviewVotes.FirstOrDefaultAsync(v => v.ReviewId == reviewId && v.VoterUserId == userId);

            return Json(new
            {
                success = true,
                upvotes,
                downvotes,
                userVote = userVote == null ? (bool?)null : userVote.IsUpvote,
            });
        }
    }
}
