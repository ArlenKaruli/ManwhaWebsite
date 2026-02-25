namespace ManwhaWebsite.Models
{
    public class Manhwa
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string CoverImageUrl { get; set; }
        public string Status { get; set; }
        public DateTime LastUpdated { get; set; }
        public int ViewCount { get; set; }
        public double Rating { get; set; }
    }
}
