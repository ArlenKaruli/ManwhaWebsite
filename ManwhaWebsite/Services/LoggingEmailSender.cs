using ManwhaWebsite.Models;
using Microsoft.AspNetCore.Identity;

namespace ManwhaWebsite.Services
{
    public class LoggingEmailSender : IEmailSender<ApplicationUser>
    {
        private readonly ILogger<LoggingEmailSender> _logger;

        public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
        {
            _logger.LogWarning(
                "DEV EMAIL — Confirm account for {Email}: {Link}",
                email, confirmationLink);
            return Task.CompletedTask;
        }

        public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
        {
            _logger.LogWarning(
                "DEV EMAIL — Password reset for {Email}: {Link}",
                email, resetLink);
            return Task.CompletedTask;
        }

        public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
        {
            _logger.LogWarning(
                "DEV EMAIL — Password reset code for {Email}: {Code}",
                email, resetCode);
            return Task.CompletedTask;
        }
    }
}
