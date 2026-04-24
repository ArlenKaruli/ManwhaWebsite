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
            SendEmailAsync(email, "Welcome to ManhwaVault — confirm your email",
                $"""
                <!DOCTYPE html>
                <html lang="en">
                <head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
                <body style="margin:0;padding:0;background-color:#0d0d12;font-family:'DM Sans',Arial,sans-serif;-webkit-font-smoothing:antialiased;">
                  <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#0d0d12;padding:48px 16px;">
                    <tr><td align="center">
                      <table width="100%" cellpadding="0" cellspacing="0" style="max-width:520px;">

                        <!-- Logo -->
                        <tr><td align="center" style="padding-bottom:36px;">
                          <span style="font-family:Georgia,'Times New Roman',serif;font-size:28px;font-weight:700;letter-spacing:3px;color:#eaeaf0;text-transform:uppercase;">
                            MANHWA<span style="color:#e8445a;">VAULT</span>
                          </span>
                        </td></tr>

                        <!-- Card -->
                        <tr><td style="background-color:#1e1e2e;border:1px solid rgba(255,255,255,0.06);border-radius:14px;padding:44px 40px;">

                          <!-- Heading -->
                          <p style="margin:0 0 8px;font-size:11px;font-weight:600;letter-spacing:2px;text-transform:uppercase;color:#e8445a;">Welcome aboard</p>
                          <h1 style="margin:0 0 16px;font-size:26px;font-weight:700;color:#eaeaf0;line-height:1.2;">
                            Hey {user.DisplayName ?? user.UserName}!
                          </h1>
                          <p style="margin:0 0 28px;font-size:15px;line-height:1.7;color:#7878a0;">
                            Thanks for signing up to <strong style="color:#eaeaf0;">ManhwaVault</strong> — your new home for discovering, tracking, and diving into the best manhwa out there. One quick step before you get started:
                          </p>

                          <!-- Button -->
                          <table cellpadding="0" cellspacing="0" style="margin-bottom:32px;">
                            <tr><td align="center" style="background-color:#e8445a;border-radius:8px;box-shadow:0 4px 24px rgba(232,68,90,0.35);">
                              <a href="{confirmationLink}" style="display:inline-block;padding:14px 36px;font-size:15px;font-weight:600;color:#ffffff;text-decoration:none;letter-spacing:0.5px;">
                                Confirm My Email
                              </a>
                            </td></tr>
                          </table>

                          <!-- Fallback link -->
                          <p style="margin:0;font-size:12px;color:#4a4a6a;line-height:1.6;">
                            If the button doesn't work, copy and paste this link into your browser:<br>
                            <a href="{confirmationLink}" style="color:#e8445a;word-break:break-all;">{confirmationLink}</a>
                          </p>

                        </td></tr>

                        <!-- Footer -->
                        <tr><td align="center" style="padding-top:28px;">
                          <p style="margin:0;font-size:12px;color:#4a4a6a;line-height:1.6;">
                            If you didn't create a ManhwaVault account, you can safely ignore this email.<br>
                            &copy; {DateTime.UtcNow.Year} ManhwaVault. All rights reserved.
                          </p>
                        </td></tr>

                      </table>
                    </td></tr>
                  </table>
                </body>
                </html>
                """);

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
