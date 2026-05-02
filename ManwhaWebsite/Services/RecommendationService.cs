using ManwhaWebsite.Data;
using ManwhaWebsite.Models;
using ManwhaWebsite.Models.ManhwaVault.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ManwhaWebsite.Services
{
    public class RecommendationService
    {
        private readonly ApplicationDbContext _db;
        private readonly AniListService _aniList;
        private readonly IMemoryCache _cache;

        public RecommendationService(ApplicationDbContext db, AniListService aniList, IMemoryCache cache)
        {
            _db = db;
            _aniList = aniList;
            _cache = cache;
        }

        public async Task<List<Manhwa>> GetRecommendationsAsync(string userId, int count = 20)
        {
            var cacheKey = $"recommendations_{userId}";
            if (_cache.TryGetValue(cacheKey, out List<Manhwa>? cached) && cached is not null)
                return cached;

            var readingList = await _db.UserReadingLists
                .Where(r => r.UserId == userId)
                .ToListAsync();

            var ratings = await _db.UserManhwaRatings
                .Where(r => r.UserId == userId)
                .ToDictionaryAsync(r => r.AniListId, r => r.Score);

            // Cold start: fall back to candidate pool sorted by popularity
            if (readingList.Count < 3)
            {
                var pool = await _aniList.GetCandidatePoolAsync();
                var fallback = pool.Take(count).ToList();
                _cache.Set(cacheKey, fallback, TimeSpan.FromMinutes(30));
                return fallback;
            }

            // Build genre preference vector
            var genreVector = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var knownIds = new HashSet<int>();

            foreach (var entry in readingList)
            {
                knownIds.Add(entry.AniListId);

                double statusWeight = entry.Status switch
                {
                    ReadingStatus.Completed  => 1.0,
                    ReadingStatus.Reading    => 0.8,
                    ReadingStatus.PlanToRead => 0.4,
                    ReadingStatus.Dropped    => -0.6,
                    _                        => 0.0,
                };

                double ratingMult = 1.0;
                if (ratings.TryGetValue(entry.AniListId, out int score) && score > 0)
                    ratingMult = score / 5.0;

                // Fetch genres for this entry from AniList (cached per title)
                var detail = await _aniList.GetMangaDetailAsync(entry.AniListId);
                if (detail?.Genres == null) continue;

                foreach (var genre in detail.Genres)
                {
                    genreVector.TryGetValue(genre, out double current);
                    genreVector[genre] = current + statusWeight * ratingMult;
                }
            }

            // Score candidates
            var candidates = await _aniList.GetCandidatePoolAsync();

            var scored = candidates
                .Where(c => !knownIds.Contains(c.Id))
                .Select(c =>
                {
                    var genreSum = c.Genres.Sum(g =>
                        genreVector.TryGetValue(g, out double w) ? w : 0.0);

                    var normalised = c.Genres.Count > 0
                        ? genreSum / Math.Sqrt(c.Genres.Count)
                        : 0.0;

                    var score = normalised + 0.05 * c.Rating;
                    return (manhwa: c, score);
                })
                .OrderByDescending(x => x.score)
                .Take(count)
                .Select(x => x.manhwa)
                .ToList();

            _cache.Set(cacheKey, scored, TimeSpan.FromMinutes(30));
            return scored;
        }
    }
}
