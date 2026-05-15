using ManwhaWebsite.Models;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ManwhaWebsite.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IDataProtectionKeyContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Manhwa> Manhwas { get; set; }
        public DbSet<UserManhwaRating> UserManhwaRatings { get; set; }
        public DbSet<UserManhwaReview> UserManhwaReviews { get; set; }
        public DbSet<UserReadingList> UserReadingLists { get; set; }
        public DbSet<ReviewVote> ReviewVotes { get; set; }
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
        public DbSet<PendingRegistration> PendingRegistrations { get; set; }
    }
}
