namespace ManwhaWebsite.Models
{
    public class CharacterInfo
    {
        public string Name { get; set; } = "";
        public string ImageUrl { get; set; } = "";
    }

    public class MangaDetailViewModel
    {
        public int AniListId { get; set; }
        public string Title { get; set; } = "";
        public string? AlternativeTitle { get; set; }
        public string Description { get; set; } = "";
        public string CoverImageUrl { get; set; } = "";
        public string? BannerImageUrl { get; set; }
        public double Score { get; set; }
        public int ScoreRank { get; set; }
        public int PopularityRank { get; set; }
        public int Popularity { get; set; }
        public int Favourites { get; set; }
        public string Status { get; set; } = "";
        public int? Chapters { get; set; }
        public int? Volumes { get; set; }
        public int? StartYear { get; set; }
        public List<string> Genres { get; set; } = new();
        public List<CharacterInfo> Characters { get; set; } = new();
        public List<UserManhwaReview> Reviews { get; set; } = new();
        public int? CurrentUserRating { get; set; }
        public ReadingStatus? CurrentUserReadingStatus { get; set; }
    }
}
