using ManwhaWebsite.Data;
using ManwhaWebsite.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ManwhaWebsite.Controllers
{
    [Route("users")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public UsersController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(string? q)
        {
            ViewData["Title"] = string.IsNullOrWhiteSpace(q) ? "Find Users" : $"Users: {q}";

            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
                return View(new UserSearchViewModel { Query = q?.Trim() ?? "", Results = new() });

            var query = q.Trim();
            var users = await _db.Users
                .AsNoTracking()
                .Where(u => u.DisplayName.Contains(query))
                .OrderBy(u => u.DisplayName)
                .Take(40)
                .ToListAsync();

            if (!users.Any())
                return View(new UserSearchViewModel { Query = query, Results = new() });

            var userIds = users.Select(u => u.Id).ToList();

            var manhwaCounts = await _db.UserReadingLists
                .Where(rl => userIds.Contains(rl.UserId) &&
                    (rl.Status == ReadingStatus.Reading || rl.Status == ReadingStatus.Completed))
                .GroupBy(rl => rl.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);

            var reviewCounts = await _db.UserManhwaReviews
                .Where(r => userIds.Contains(r.UserId))
                .GroupBy(r => r.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);

            var results = users.Select(u => new UserCardViewModel
            {
                UserId = u.Id,
                DisplayName = string.IsNullOrEmpty(u.DisplayName) ? "User" : u.DisplayName,
                ProfilePictureUrl = u.ProfilePictureUrl,
                Bio = u.Bio,
                JoinedAt = u.JoinedAt,
                ManhwaCount = manhwaCounts.GetValueOrDefault(u.Id, 0),
                ReviewCount = reviewCounts.GetValueOrDefault(u.Id, 0)
            }).ToList();

            return View(new UserSearchViewModel { Query = query, Results = results });
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> Profile(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var readingListEntries = await _db.UserReadingLists
                .AsNoTracking()
                .Where(rl => rl.UserId == userId)
                .ToListAsync();

            var manhwasRead = readingListEntries.Count(e =>
                e.Status == ReadingStatus.Reading || e.Status == ReadingStatus.Completed);
            var planToRead = readingListEntries.Count(e => e.Status == ReadingStatus.PlanToRead);
            var dropped = readingListEntries.Count(e => e.Status == ReadingStatus.Dropped);

            int totalChapters = 0;
            foreach (var entry in readingListEntries.Where(e =>
                e.Status == ReadingStatus.Reading || e.Status == ReadingStatus.Completed))
            {
                if (entry.Chapters.HasValue) totalChapters += entry.Chapters.Value;
                else if (entry.MdChapters.HasValue) totalChapters += entry.MdChapters.Value;
            }

            var daysRead = totalChapters * 7.0 / 1440.0;
            var daysReadStr = daysRead >= 1 ? daysRead.ToString("F1") : daysRead.ToString("F2");

            var reviewCount = await _db.UserManhwaReviews.CountAsync(r => r.UserId == userId);

            var myReviewIds = await _db.UserManhwaReviews
                .Where(r => r.UserId == userId)
                .Select(r => r.Id)
                .ToListAsync();

            var upvotes   = await _db.ReviewVotes.CountAsync(v => myReviewIds.Contains(v.ReviewId) && v.IsUpvote);
            var downvotes = await _db.ReviewVotes.CountAsync(v => myReviewIds.Contains(v.ReviewId) && !v.IsUpvote);

            var recentReviews = await _db.UserManhwaReviews
                .AsNoTracking()
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewData["Title"] = $"{user.DisplayName}'s Profile";

            return View(new UserProfileViewModel
            {
                UserId = user.Id,
                DisplayName = string.IsNullOrEmpty(user.DisplayName) ? "User" : user.DisplayName,
                ProfilePictureUrl = user.ProfilePictureUrl,
                Bio = user.Bio,
                JoinedAt = user.JoinedAt,
                ManhwasRead = manhwasRead,
                PlanToRead = planToRead,
                Dropped = dropped,
                ChaptersRead = totalChapters,
                DaysRead = daysReadStr,
                ReviewCount = reviewCount,
                UpvotesReceived = upvotes,
                DownvotesReceived = downvotes,
                RecentReviews = recentReviews
            });
        }
    }
}
