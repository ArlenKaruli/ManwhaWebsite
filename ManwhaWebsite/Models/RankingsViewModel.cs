namespace ManwhaWebsite.Models
{
    public class RankingsViewModel
    {
        public List<Manhwa> TopRated { get; set; } = new();
        public List<Manhwa> Trending { get; set; } = new();
    }
}
