namespace ManwhaWebsite.Models
{
    public class UserManhwaRating
    {
        public int Id { get; set; }
        public string UserId { get; set; } = "";
        public int AniListId { get; set; }
        public int Score { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
