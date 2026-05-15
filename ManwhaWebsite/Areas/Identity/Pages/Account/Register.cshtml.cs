using ManwhaWebsite.Data;
using ManwhaWebsite.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ManwhaWebsite.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender<ApplicationUser> _emailSender;
        private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
        private readonly ApplicationDbContext _db;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IEmailSender<ApplicationUser> emailSender,
            IPasswordHasher<ApplicationUser> passwordHasher,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _passwordHasher = passwordHasher;
            _db = db;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required]
            [StringLength(50, MinimumLength = 2)]
            [Display(Name = "Display Name")]
            public string DisplayName { get; set; } = string.Empty;

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 8)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (!ModelState.IsValid)
                return Page();

            var normalizedEmail = Input.Email.Trim().ToUpperInvariant();

            var existingUser = await _userManager.FindByEmailAsync(Input.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "An account with this email already exists.");
                return Page();
            }

            // Remove any previous unconfirmed attempt for this email
            var existing = await _db.PendingRegistrations
                .FirstOrDefaultAsync(p => p.Email.ToUpper() == normalizedEmail);
            if (existing != null)
                _db.PendingRegistrations.Remove(existing);

            var tempUser = new ApplicationUser();
            var pending = new PendingRegistration
            {
                Email = Input.Email.Trim(),
                DisplayName = Input.DisplayName,
                PasswordHash = _passwordHasher.HashPassword(tempUser, Input.Password),
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            _db.PendingRegistrations.Add(pending);
            await _db.SaveChangesAsync();

            var confirmationLink = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", pendingId = pending.Id, returnUrl },
                protocol: Request.Scheme)!;

            var userForEmail = new ApplicationUser { Email = Input.Email, DisplayName = Input.DisplayName };
            await _emailSender.SendConfirmationLinkAsync(userForEmail, Input.Email, confirmationLink);

            return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl });
        }
    }
}
