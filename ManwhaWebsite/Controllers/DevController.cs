using ManwhaWebsite.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ManwhaWebsite.Controllers
{
    public class DevController : Controller
    {
        private readonly IEmailSender<ApplicationUser> _email;

        public DevController(IEmailSender<ApplicationUser> email)
        {
            _email = email;
        }

        public async Task<IActionResult> TestEmail(string to = "akaruli@actirana.edu.al")
        {
            var fakeUser = new ApplicationUser
            {
                UserName = "testuser",
                DisplayName = "Test User",
                Email = to
            };

            await _email.SendConfirmationLinkAsync(fakeUser, to, "https://example.com/confirm?token=preview");
            return Content($"Confirmation email sent to {to}.");
        }
    }
}
