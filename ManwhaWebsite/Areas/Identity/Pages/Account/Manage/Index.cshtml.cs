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

        public IndexModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public string Username { get; set; } = string.Empty;

        [TempData]
        public string? StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Display(Name = "Display Name")]
            [MaxLength(50)]
            public string? DisplayName { get; set; }

            [Phone]
            [Display(Name = "Phone number")]
            public string? PhoneNumber { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            Username = await _userManager.GetUserNameAsync(user) ?? string.Empty;
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
