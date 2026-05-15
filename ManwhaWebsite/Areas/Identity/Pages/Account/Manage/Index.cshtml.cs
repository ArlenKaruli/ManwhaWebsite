using ManwhaWebsite.Data;
using ManwhaWebsite.Models;
using ManwhaWebsite.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ManwhaWebsite.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _db;
        private readonly MangaUpdatesService _mangaUpdates;
        private readonly BlobStorageService? _blob;

        private static readonly HashSet<string> _allowedExtensions =
            new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        public IndexModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager,
            IWebHostEnvironment env, ApplicationDbContext db, MangaUpdatesService mangaUpdates,
            BlobStorageService? blob = null)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _env = env;
            _db = db;
            _mangaUpdates = mangaUpdates;
            _blob = blob;
        }

        public string Username { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }

        public int StatManhwasRead { get; set; }
        public int StatChaptersRead { get; set; }
        public string StatDaysRead { get; set; } = "0";
        public int StatCommentsMade { get; set; }
        public int StatUpvotesReceived { get; set; }
        public int StatDownvotesReceived { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [BindProperty]
        public IFormFile? ProfilePictureFile { get; set; }

        [BindProperty]
        public bool RemoveProfilePicture { get; set; }

        public class InputModel
        {
            [Display(Name = "Display Name")]
            [MaxLength(30)]
            public string? DisplayName { get; set; }

            [Phone]
            [Display(Name = "Phone number")]
            public string? PhoneNumber { get; set; }

            [Display(Name = "Bio")]
            [MaxLength(160)]
            public string? Bio { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            Username = await _userManager.GetUserNameAsync(user) ?? string.Empty;
            ProfilePictureUrl = user.ProfilePictureUrl;
            Input = new InputModel
            {
                DisplayName = user.DisplayName,
                PhoneNumber = await _userManager.GetPhoneNumberAsync(user),
                Bio = user.Bio
            };
        }

        private async Task LoadStatsAsync(string userId)
        {
            var entries = await _db.UserReadingLists
                .Where(e => e.UserId == userId)
                .ToListAsync();

            // Both Reading and Completed count as "read"
            StatManhwasRead = entries.Count(e =>
                e.Status == ReadingStatus.Reading || e.Status == ReadingStatus.Completed);

            // Refresh stale MangaUpdates chapter counts for Reading + Completed entries (max 10 per load)
            var stale = entries
                .Where(e => (e.Status == ReadingStatus.Reading || e.Status == ReadingStatus.Completed) &&
                            !e.Chapters.HasValue &&
                            (e.MdChaptersCachedAt == null ||
                             e.MdChaptersCachedAt < DateTime.UtcNow.AddHours(-24)))
                .Take(10)
                .ToList();

            foreach (var entry in stale)
            {
                var ch = await _mangaUpdates.GetTotalChaptersByTitleAsync(entry.Title);
                entry.MdChapters = ch;
                entry.MdChaptersCachedAt = DateTime.UtcNow;
            }

            if (stale.Count > 0)
                await _db.SaveChangesAsync();

            // Sum chapters for Reading + Completed: user-entered value takes priority, MangaUpdates as fallback
            int total = 0;
            foreach (var entry in entries.Where(e =>
                e.Status == ReadingStatus.Reading || e.Status == ReadingStatus.Completed))
            {
                if (entry.Chapters.HasValue)
                    total += entry.Chapters.Value;
                else if (entry.MdChapters.HasValue)
                    total += entry.MdChapters.Value;
            }

            StatChaptersRead = total;

            var minutes = total * 7.0;
            var days = minutes / 1440.0;
            StatDaysRead = days >= 1 ? days.ToString("F1") : days.ToString("F2");

            // Comments Made
            StatCommentsMade = await _db.UserManhwaReviews.CountAsync(r => r.UserId == userId);

            // Votes received on this user's reviews
            var myReviewIds = await _db.UserManhwaReviews
                .Where(r => r.UserId == userId)
                .Select(r => r.Id)
                .ToListAsync();

            StatUpvotesReceived   = await _db.ReviewVotes.CountAsync(v => myReviewIds.Contains(v.ReviewId) && v.IsUpvote);
            StatDownvotesReceived = await _db.ReviewVotes.CountAsync(v => myReviewIds.Contains(v.ReviewId) && !v.IsUpvote);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();
            await LoadAsync(user);
            await LoadStatsAsync(user.Id);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            if (RemoveProfilePicture && !string.IsNullOrEmpty(user.ProfilePictureUrl))
            {
                if (_blob != null)
                {
                    var blobName = user.ProfilePictureUrl!.StartsWith("/profile-picture/")
                        ? user.ProfilePictureUrl["/profile-picture/".Length..]
                        : user.Id;
                    await _blob.DeleteAsync(blobName);
                }
                else
                {
                    var oldPath = Path.Combine(_env.WebRootPath, user.ProfilePictureUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }
                user.ProfilePictureUrl = null;
                var removeResult = await _userManager.UpdateAsync(user);
                if (!removeResult.Succeeded)
                {
                    StatusMessage = "Error: Failed to remove profile picture.";
                    return RedirectToPage();
                }
            }

            if (!RemoveProfilePicture && ProfilePictureFile != null && ProfilePictureFile.Length > 0)
            {
                var ext = Path.GetExtension(ProfilePictureFile.FileName);
                if (!_allowedExtensions.Contains(ext))
                {
                    StatusMessage = "Error: Only JPG, PNG, GIF, and WebP images are allowed.";
                    return RedirectToPage();
                }
                if (ProfilePictureFile.Length > 5 * 1024 * 1024)
                {
                    StatusMessage = "Error: Image must be smaller than 5 MB.";
                    return RedirectToPage();
                }

                string pictureUrl;
                if (_blob != null)
                {
                    var blobName = user.Id + ext;
                    var contentType = ProfilePictureFile.ContentType;
                    using var stream = ProfilePictureFile.OpenReadStream();
                    await _blob.UploadAsync(blobName, stream, contentType);
                    pictureUrl = "/profile-picture/" + blobName;
                }
                else
                {
                    var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "profile-pictures");
                    Directory.CreateDirectory(uploadDir);
                    var fileName = user.Id + ext;
                    var filePath = Path.Combine(uploadDir, fileName);
                    foreach (var oldExt in _allowedExtensions)
                    {
                        var old = Path.Combine(uploadDir, user.Id + oldExt);
                        if (old != filePath && System.IO.File.Exists(old))
                            System.IO.File.Delete(old);
                    }
                    using (var stream = new FileStream(filePath, FileMode.Create))
                        await ProfilePictureFile.CopyToAsync(stream);
                    pictureUrl = "/uploads/profile-pictures/" + fileName;
                }

                user.ProfilePictureUrl = pictureUrl;
                var picResult = await _userManager.UpdateAsync(user);
                if (!picResult.Succeeded)
                {
                    StatusMessage = "Error: Failed to save profile picture.";
                    return RedirectToPage();
                }
            }

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var result = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!result.Succeeded)
                {
                    StatusMessage = "Error: Failed to update phone number.";
                    return RedirectToPage();
                }
            }

            if (Input.DisplayName != user.DisplayName || Input.Bio != user.Bio)
            {
                user.DisplayName = Input.DisplayName ?? string.Empty;
                user.Bio = Input.Bio?.Trim();
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    StatusMessage = "Error: Failed to update profile.";
                    return RedirectToPage();
                }
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated.";
            return RedirectToPage();
        }
    }
}
