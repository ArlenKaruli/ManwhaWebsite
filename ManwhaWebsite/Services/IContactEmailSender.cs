namespace ManwhaWebsite.Services
{
    /// <summary>
    /// Abstraction over whichever email backend is active (SMTP or Resend HTTP API).
    /// Allows StaticController to send contact-form emails without depending on a
    /// concrete sender class.
    /// </summary>
    public interface IContactEmailSender
    {
        Task SendContactMessageAsync(string fromName, string fromEmail, string subject, string message);
    }
}
