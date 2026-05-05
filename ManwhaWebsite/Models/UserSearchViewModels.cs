namespace ManwhaWebsite.Models
{
    public class UserSearchViewModel
    {
        public string Query { get; set; } = string.Empty;
        public List<UserCardViewModel> Results { get; set; } = new();
    }

    public class UserCardViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public string? Bio { get; set; }
        public DateTime JoinedAt { get; set; }
        public int ManhwaCount { get; set; }
        public int ReviewCount { get; set; }
    }

    public class UserProfileViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public string? Bio { get; set; }
        public DateTime JoinedAt { get; set; }
        public int ManhwasRead { get; set; }
        public int PlanToRead { get; set; }
        public int Dropped { get; set; }
        public int ChaptersRead { get; set; }
        public string DaysRead { get; set; } = "0";
        public int ReviewCount { get; set; }
        public int UpvotesReceived { get; set; }
        public int DownvotesReceived { get; set; }
        public List<UserManhwaReview> RecentReviews { get; set; } = new();
    }
}
