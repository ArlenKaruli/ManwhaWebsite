using ManwhaWebsite.Models;
using Microsoft.AspNetCore.Identity;
using System.Net;
using System.Net.Mail;

namespace ManwhaWebsite.Services
{
    public class SmtpEmailSender : IEmailSender<ApplicationUser>
    {
        private readonly IConfiguration _config;

        public SmtpEmailSender(IConfiguration config)
        {
            _config = config;
        }

        public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
            SendEmailAsync(email, "Confirm your email",
                $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.");

        public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
            SendEmailAsync(email, "Reset your password",
                $"Reset your password by <a href='{resetLink}'>clicking here</a>.");

        public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
            SendEmailAsync(email, "Reset your password",
                $"Your password reset code is: {resetCode}");

        private Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var section = _config.GetSection("EmailSettings");
            var host = section["SmtpHost"] ?? throw new InvalidOperationException("EmailSettings:SmtpHost is not configured.");
            var port = int.Parse(section["SmtpPort"] ?? "587");
            var enableSsl = bool.Parse(section["EnableSsl"] ?? "true");
            var senderEmail = section["SenderEmail"] ?? throw new InvalidOperationException("EmailSettings:SenderEmail is not configured.");
            var senderName = section["SenderName"] ?? "Manhwa List";
            var username = section["Username"] ?? string.Empty;
            var password = section["Password"] ?? string.Empty;

            var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(username, password)
            };

            var message = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            return client.SendMailAsync(message);
        }
    }
}
