using ManwhaWebsite.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace ManwhaWebsite.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _env;

        private static readonly HashSet<string> _allowedExtensions =
            new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        public IndexModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IWebHostEnvironment env)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _env = env;
        }

        public string Username { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }

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
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            Username = await _userManager.GetUserNameAsync(user) ?? string.Empty;
            ProfilePictureUrl = user.ProfilePictureUrl;
            Input = new InputModel
            {
                DisplayName = user.DisplayName,
                PhoneNumber = await _userManager.GetPhoneNumberAsync(user)
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();
            await LoadAsync(user);
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
                var oldPath = Path.Combine(_env.WebRootPath, user.ProfilePictureUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
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

                var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "profile-pictures");
                Directory.CreateDirectory(uploadDir);

                var fileName = user.Id + ext;
                var filePath = Path.Combine(uploadDir, fileName);

                // Delete old file if extension changed
                foreach (var oldExt in _allowedExtensions)
                {
                    var old = Path.Combine(uploadDir, user.Id + oldExt);
                    if (old != filePath && System.IO.File.Exists(old))
                        System.IO.File.Delete(old);
                }

                using (var stream = new FileStream(filePath, FileMode.Create))
                    await ProfilePictureFile.CopyToAsync(stream);

                user.ProfilePictureUrl = "/uploads/profile-pictures/" + fileName;
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

            if (Input.DisplayName != user.DisplayName)
            {
                user.DisplayName = Input.DisplayName ?? string.Empty;
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    StatusMessage = "Error: Failed to update display name.";
                    return RedirectToPage();
                }
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated.";
            return RedirectToPage();
        }
    }
}
