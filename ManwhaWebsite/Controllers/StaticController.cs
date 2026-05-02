using ManwhaWebsite.Services;
using Microsoft.AspNetCore.Mvc;

namespace ManwhaWebsite.Controllers
{
    public class StaticController : Controller
    {
        private readonly SmtpEmailSender? _mailer;

        public StaticController(SmtpEmailSender? mailer = null)
        {
            _mailer = mailer;
        }

        public IActionResult About()   => View();
        public IActionResult Contact() => View();
        public IActionResult Privacy() => View();
        public IActionResult Terms()   => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(string name, string email, string subject, string message)
        {
            if (_mailer == null)
                return Json(new { ok = false, error = "Email is not configured on this server." });

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(message))
                return Json(new { ok = false, error = "Please fill in all required fields." });

            await _mailer.SendContactMessageAsync(name, email, subject ?? "General Question", message);
            return Json(new { ok = true });
        }
    }
}
