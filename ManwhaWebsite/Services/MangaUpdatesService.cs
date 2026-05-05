using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;

namespace ManwhaWebsite.Services
{
    public class MangaUpdatesService
    {
        private readonly HttpClient _http;
        private readonly IMemoryCache _cache;

        public MangaUpdatesService(HttpClient http, IMemoryCache cache)
        {
            _http = http;
            _cache = cache;
        }

        public async Task<int?> GetTotalChaptersByTitleAsync(string title)
        {
            var cacheKey = $"mu_chapters_{title.ToLowerInvariant()}";
            if (_cache.TryGetValue(cacheKey, out int? cached))
                return cached;

            int? result = null;
            try { result = await FetchFromMangaUpdatesAsync(title); }
            catch { }

            _cache.Set(cacheKey, result, TimeSpan.FromDays(7));
            return result;
        }
        private async Task<int?> FetchFromMangaUpdatesAsync(string title)
        {
            var searchBody = JsonSerializer.Serialize(new { search = title, type = "manhwa" });
            var searchResp = await _http.PostAsync(
                "https://api.mangaupdates.com/v1/series/search",
                new StringContent(searchBody, Encoding.UTF8, "application/json"));

            if (!searchResp.IsSuccessStatusCode) return null;

            var searchJson = await searchResp.Content.ReadAsStringAsync();
            using var searchDoc = JsonDocument.Parse(searchJson);
            var results = searchDoc.RootElement.GetProperty("results");
            if (results.GetArrayLength() == 0) return null;

            var seriesId = results[0].GetProperty("record").GetProperty("series_id").GetInt64();

            var detailResp = await _http.GetAsync($"https://api.mangaupdates.com/v1/series/{seriesId}");
            if (!detailResp.IsSuccessStatusCode) return null;

            var detailJson = await detailResp.Content.ReadAsStringAsync();
            using var detailDoc = JsonDocument.Parse(detailJson);
            var root = detailDoc.RootElement;

            // status field contains the true total, e.g. "652 Chapters (Ongoing)"
            if (root.TryGetProperty("status", out var statusEl))
            {
                var statusStr = statusEl.GetString() ?? "";
                var m = Regex.Match(statusStr, @"^(\d+)");
                if (m.Success && int.TryParse(m.Groups[1].Value, out var total))
                    return total;
            }

            if (root.TryGetProperty("latest_chapter", out var lcEl) &&
                lcEl.ValueKind == JsonValueKind.Number)
                return lcEl.GetInt32();

            return null;
        }
    }
}
