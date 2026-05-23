using ManwhaWebsite.Models;
using Microsoft.AspNetCore.Identity;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ManwhaWebsite.Services
{
    /// <summary>
    /// Sends transactional email via Resend's HTTP API (api.resend.com/emails).
    /// No SMTP ports are used, so it works on Railway's free tier.
    ///
    /// Required configuration:
    ///   Resend__ApiKey             — Resend API key (re_xxxx…)
    ///   EmailSettings__SenderEmail — verified sender address, e.g. noreply@yourdomain.com
    ///   EmailSettings__SenderName  — display name shown in the From field
    /// </summary>
    public class ResendEmailSender : IEmailSender<ApplicationUser>, IContactEmailSender
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;

        public ResendEmailSender(HttpClient http, IConfiguration config)
        {
            _http = http;
            _config = config;

            var apiKey = _config["Resend:ApiKey"]
                ?? throw new InvalidOperationException("Resend:ApiKey is not configured.");

            _http.BaseAddress = new Uri("https://api.resend.com/");
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);
        }

        // ── IEmailSender<ApplicationUser> ─────────────────────────────────────

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
            SendEmailAsync(email, "Reset your ManhwaVault password",
                $"""
                <!DOCTYPE html>
                <html lang="en">
                <head><meta charset="UTF-8"></head>
                <body style="margin:0;padding:0;background-color:#0d0d12;font-family:'DM Sans',Arial,sans-serif;">
                  <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#0d0d12;padding:48px 16px;">
                    <tr><td align="center">
                      <table width="100%" cellpadding="0" cellspacing="0" style="max-width:520px;">
                        <tr><td align="center" style="padding-bottom:36px;">
                          <span style="font-family:Georgia,'Times New Roman',serif;font-size:28px;font-weight:700;letter-spacing:3px;color:#eaeaf0;text-transform:uppercase;">
                            MANHWA<span style="color:#e8445a;">VAULT</span>
                          </span>
                        </td></tr>
                        <tr><td style="background-color:#1e1e2e;border:1px solid rgba(255,255,255,0.06);border-radius:14px;padding:44px 40px;">
                          <p style="margin:0 0 8px;font-size:11px;font-weight:600;letter-spacing:2px;text-transform:uppercase;color:#e8445a;">Password reset</p>
                          <h1 style="margin:0 0 16px;font-size:26px;font-weight:700;color:#eaeaf0;line-height:1.2;">Reset your password</h1>
                          <p style="margin:0 0 28px;font-size:15px;line-height:1.7;color:#7878a0;">
                            We received a request to reset your password. Click the button below to choose a new one.
                          </p>
                          <table cellpadding="0" cellspacing="0" style="margin-bottom:32px;">
                            <tr><td align="center" style="background-color:#e8445a;border-radius:8px;box-shadow:0 4px 24px rgba(232,68,90,0.35);">
                              <a href="{resetLink}" style="display:inline-block;padding:14px 36px;font-size:15px;font-weight:600;color:#ffffff;text-decoration:none;letter-spacing:0.5px;">
                                Reset Password
                              </a>
                            </td></tr>
                          </table>
                          <p style="margin:0;font-size:12px;color:#4a4a6a;line-height:1.6;">
                            If you didn't request a password reset, you can safely ignore this email.<br>
                            <a href="{resetLink}" style="color:#e8445a;word-break:break-all;">{resetLink}</a>
                          </p>
                        </td></tr>
                        <tr><td align="center" style="padding-top:28px;">
                          <p style="margin:0;font-size:12px;color:#4a4a6a;">&copy; {DateTime.UtcNow.Year} ManhwaVault. All rights reserved.</p>
                        </td></tr>
                      </table>
                    </td></tr>
                  </table>
                </body>
                </html>
                """);

        public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
            SendEmailAsync(email, "Your ManhwaVault password reset code",
                $"""
                <!DOCTYPE html>
                <html lang="en">
                <head><meta charset="UTF-8"></head>
                <body style="margin:0;padding:0;background-color:#0d0d12;font-family:'DM Sans',Arial,sans-serif;">
                  <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#0d0d12;padding:48px 16px;">
                    <tr><td align="center">
                      <table width="100%" cellpadding="0" cellspacing="0" style="max-width:520px;">
                        <tr><td align="center" style="padding-bottom:36px;">
                          <span style="font-family:Georgia,'Times New Roman',serif;font-size:28px;font-weight:700;letter-spacing:3px;color:#eaeaf0;text-transform:uppercase;">
                            MANHWA<span style="color:#e8445a;">VAULT</span>
                          </span>
                        </td></tr>
                        <tr><td style="background-color:#1e1e2e;border:1px solid rgba(255,255,255,0.06);border-radius:14px;padding:44px 40px;">
                          <p style="margin:0 0 8px;font-size:11px;font-weight:600;letter-spacing:2px;text-transform:uppercase;color:#e8445a;">Password reset</p>
                          <h1 style="margin:0 0 24px;font-size:26px;font-weight:700;color:#eaeaf0;">Your reset code</h1>
                          <p style="margin:0 0 24px;font-size:15px;line-height:1.7;color:#7878a0;">Use the code below to reset your password:</p>
                          <div style="background:#12121c;border:1px solid rgba(232,68,90,0.3);border-radius:8px;padding:20px;text-align:center;margin-bottom:28px;">
                            <span style="font-family:monospace;font-size:32px;font-weight:700;letter-spacing:8px;color:#e8445a;">{resetCode}</span>
                          </div>
                          <p style="margin:0;font-size:12px;color:#4a4a6a;">If you didn't request this, you can safely ignore this email.</p>
                        </td></tr>
                        <tr><td align="center" style="padding-top:28px;">
                          <p style="margin:0;font-size:12px;color:#4a4a6a;">&copy; {DateTime.UtcNow.Year} ManhwaVault. All rights reserved.</p>
                        </td></tr>
                      </table>
                    </td></tr>
                  </table>
                </body>
                </html>
                """);

        // ── IContactEmailSender ───────────────────────────────────────────────

        public Task SendContactMessageAsync(string fromName, string fromEmail, string subject, string message)
        {
            var section = _config.GetSection("EmailSettings");
            var senderEmail = section["SenderEmail"]
                ?? throw new InvalidOperationException("EmailSettings:SenderEmail is not configured.");

            var htmlBody = $"""
                <!DOCTYPE html>
                <html><body style="font-family:Arial,sans-serif;background:#0d0d12;color:#eaeaf0;padding:32px;">
                  <h2 style="color:#e8445a;">New Contact Form Submission</h2>
                  <p><strong>From:</strong> {System.Net.WebUtility.HtmlEncode(fromName)} &lt;{System.Net.WebUtility.HtmlEncode(fromEmail)}&gt;</p>
                  <p><strong>Subject:</strong> {System.Net.WebUtility.HtmlEncode(subject)}</p>
                  <hr style="border-color:#333;" />
                  <p style="white-space:pre-wrap;">{System.Net.WebUtility.HtmlEncode(message)}</p>
                </body></html>
                """;

            return SendEmailAsync(senderEmail, $"[Contact] {subject}", htmlBody, replyTo: fromEmail);
        }

        // ── Core HTTP send ────────────────────────────────────────────────────

        private async Task SendEmailAsync(string toEmail, string subject, string htmlBody, string? replyTo = null)
        {
            var section = _config.GetSection("EmailSettings");
            var senderEmail = section["SenderEmail"]
                ?? throw new InvalidOperationException("EmailSettings:SenderEmail is not configured.");
            var senderName = section["SenderName"] ?? "ManhwaVault";

            var payload = new Dictionary<string, object>
            {
                ["from"]    = $"{senderName} <{senderEmail}>",
                ["to"]      = new[] { toEmail },
                ["subject"] = subject,
                ["html"]    = htmlBody,
            };

            if (replyTo != null)
                payload["reply_to"] = replyTo;

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("emails", content);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException(
                    $"Resend API returned {(int)response.StatusCode}: {body}");
            }
        }
    }
}
