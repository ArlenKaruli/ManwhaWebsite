using ManwhaWebsite.Data;
using ManwhaWebsite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ManwhaWebsite.Controllers
{
    [Authorize]
    [Route("ReadingList")]
    public class ReadingListController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReadingListController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string filter = "all")
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var all = await _context.UserReadingLists
                .Where(e => e.UserId == user.Id)
                .OrderByDescending(e => e.UpdatedAt)
                .ToListAsync();

            var vm = new ReadingListViewModel
            {
                DisplayName     = string.IsNullOrWhiteSpace(user.DisplayName) ? (user.Email ?? "User") : user.DisplayName,
                ReadingCount    = all.Count(e => e.Status == ReadingStatus.Reading),
                CompletedCount  = all.Count(e => e.Status == ReadingStatus.Completed),
                PlanToReadCount = all.Count(e => e.Status == ReadingStatus.PlanToRead),
                DroppedCount    = all.Count(e => e.Status == ReadingStatus.Dropped),
                GroupedEntries  = new Dictionary<ReadingStatus, List<UserReadingList>>
                {
                    [ReadingStatus.Reading]    = all.Where(e => e.Status == ReadingStatus.Reading).ToList(),
                    [ReadingStatus.Completed]  = all.Where(e => e.Status == ReadingStatus.Completed).ToList(),
                    [ReadingStatus.PlanToRead] = all.Where(e => e.Status == ReadingStatus.PlanToRead).ToList(),
                    [ReadingStatus.Dropped]    = all.Where(e => e.Status == ReadingStatus.Dropped).ToList(),
                }
            };

            if (filter == "all" || string.IsNullOrEmpty(filter))
            {
                vm.ShowAll = true;
                vm.Entries = all;
            }
            else if (Enum.TryParse<ReadingStatus>(filter, ignoreCase: true, out var status))
            {
                vm.ShowAll = false;
                vm.ActiveFilter = status;
                vm.Entries = all.Where(e => e.Status == status).ToList();
            }
            else
            {
                vm.ShowAll = true;
                vm.Entries = all;
            }

            return View(vm);
        }

        [HttpPost("Add")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int aniListId, string title, string coverImageUrl, int? chapters, ReadingStatus status, string? returnUrl)
        {
            var userId = _userManager.GetUserId(User)!;
            var now = DateTime.UtcNow;

            var existing = await _context.UserReadingLists
                .FirstOrDefaultAsync(e => e.UserId == userId && e.AniListId == aniListId);

            if (existing == null)
            {
                _context.UserReadingLists.Add(new UserReadingList
                {
                    UserId = userId,
                    AniListId = aniListId,
                    Title = title,
                    CoverImageUrl = coverImageUrl,
                    Chapters = chapters,
                    Status = status,
                    AddedAt = now,
                    UpdatedAt = now,
                });
            }
            else
            {
                existing.Status = status;
                existing.Title = title;
                existing.CoverImageUrl = coverImageUrl;
                existing.Chapters = chapters;
                existing.UpdatedAt = now;
            }

            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost("MarkStatus")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkStatus([FromForm] int aniListId, [FromForm] string title, [FromForm] string coverImageUrl, [FromForm] ReadingStatus status)
        {
            var userId = _userManager.GetUserId(User)!;
            var now = DateTime.UtcNow;

            var existing = await _context.UserReadingLists
                .FirstOrDefaultAsync(e => e.UserId == userId && e.AniListId == aniListId);

            if (existing == null)
            {
                _context.UserReadingLists.Add(new UserReadingList
                {
                    UserId = userId,
                    AniListId = aniListId,
                    Title = title,
                    CoverImageUrl = coverImageUrl,
                    Status = status,
                    AddedAt = now,
                    UpdatedAt = now,
                });
            }
            else
            {
                existing.Status = status;
                existing.UpdatedAt = now;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost("Remove")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int aniListId, string? returnUrl)
        {
            var userId = _userManager.GetUserId(User)!;

            var entry = await _context.UserReadingLists
                .FirstOrDefaultAsync(e => e.UserId == userId && e.AniListId == aniListId);

            if (entry != null)
            {
                _context.UserReadingLists.Remove(entry);
                await _context.SaveChangesAsync();
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }
    }
}
