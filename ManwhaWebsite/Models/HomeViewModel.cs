namespace ManwhaWebsite.Models
{
    public class HomeViewModel
    {
        public IEnumerable<Manhwa> Trending { get; set; } = new List<Manhwa>();
        public IEnumerable<Manhwa> Popular { get; set; } = new List<Manhwa>();
        public IEnumerable<Manhwa> NewlyUpdated { get; set; } = new List<Manhwa>();
        public IEnumerable<Manhwa> Random { get; set; } = new List<Manhwa>();
    }
}
