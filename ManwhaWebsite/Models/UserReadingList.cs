namespace ManwhaWebsite.Models
{
    public enum ReadingStatus
    {
        Reading = 0,
        Completed = 1,
        PlanToRead = 2,
        Dropped = 3
    }

    public class UserReadingList
    {
        public int Id { get; set; }
        public string UserId { get; set; } = "";
        public int AniListId { get; set; }
        public string Title { get; set; } = "";
        public string CoverImageUrl { get; set; } = "";
        public ReadingStatus Status { get; set; }
        public int? Chapters { get; set; }
        public DateTime AddedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
