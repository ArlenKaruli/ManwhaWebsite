namespace ManwhaWebsite.Models
{
    public class UserManhwaReview
    {
        public int Id { get; set; }
        public string UserId { get; set; } = "";
        public string UserDisplayName { get; set; } = "";
        public string? UserProfilePicture { get; set; }
        public int AniListId { get; set; }
        public int Score { get; set; }
        public string Comment { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}
