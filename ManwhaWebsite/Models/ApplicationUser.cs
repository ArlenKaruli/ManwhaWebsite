using Microsoft.AspNetCore.Identity;

namespace ManwhaWebsite.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string DisplayName { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public string? Bio { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
