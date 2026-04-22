using Microsoft.AspNetCore.Identity;

namespace ManwhaWebsite.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string DisplayName { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
    }
}
