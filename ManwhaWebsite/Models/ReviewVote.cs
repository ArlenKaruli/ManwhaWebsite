namespace ManwhaWebsite.Models
{
    public class ReviewVote
    {
        public int Id { get; set; }
        public int ReviewId { get; set; }
        public string VoterUserId { get; set; } = "";
        public bool IsUpvote { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
