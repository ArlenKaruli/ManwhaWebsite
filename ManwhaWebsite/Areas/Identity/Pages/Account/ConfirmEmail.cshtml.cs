using ManwhaWebsite.Data;
using ManwhaWebsite.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace ManwhaWebsite.Areas.Identity.Pages.Account
{
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _db;

        public ConfirmEmailModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _db = db;
        }

        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string? pendingId, string? userId, string? code)
        {
            // New flow: confirm a pending registration
            if (pendingId != null)
            {
                if (!Guid.TryParse(pendingId, out var guid))
                    return RedirectToPage("/Index");

                var pending = await _db.PendingRegistrations.FindAsync(guid);

                if (pending == null)
                {
                    ErrorMessage = "This confirmation link is invalid or has already been used.";
                    return Page();
                }

                if (pending.ExpiresAt < DateTime.UtcNow)
                {
                    _db.PendingRegistrations.Remove(pending);
                    await _db.SaveChangesAsync();
                    ErrorMessage = "This confirmation link has expired. Please register again.";
                    return Page();
                }

                var existing = await _userManager.FindByEmailAsync(pending.Email);
                if (existing != null)
                {
                    _db.PendingRegistrations.Remove(pending);
                    await _db.SaveChangesAsync();
                    ErrorMessage = "An account with this email already exists.";
                    return Page();
                }

                var user = new ApplicationUser
                {
                    UserName = pending.Email,
                    Email = pending.Email,
                    DisplayName = pending.DisplayName,
                    EmailConfirmed = true,
                    JoinedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    ErrorMessage = "Account creation failed. Please try registering again.";
                    return Page();
                }

                user.PasswordHash = pending.PasswordHash;
                await _userManager.UpdateAsync(user);

                _db.PendingRegistrations.Remove(pending);
                await _db.SaveChangesAsync();

                await _signInManager.SignInAsync(user, isPersistent: false);
                Success = true;
                return Page();
            }

            // Legacy flow: existing unconfirmed users already in the DB
            if (userId == null || code == null)
                return RedirectToPage("/Index");

            var legacyUser = await _userManager.FindByIdAsync(userId);
            if (legacyUser == null)
                return NotFound();

            var token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var confirmResult = await _userManager.ConfirmEmailAsync(legacyUser, token);

            if (confirmResult.Succeeded)
            {
                await _signInManager.SignInAsync(legacyUser, isPersistent: false);
                Success = true;
            }

            return Page();
        }
    }
}
