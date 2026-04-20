namespace ManwhaWebsite.Models
{
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    namespace ManhwaVault.Services
    {
        public class AniListService
        {
            private readonly HttpClient _http;

            public AniListService(HttpClient http)
            {
                _http = http;
            }

            public async Task<HomeViewModel> GetHomepageDataAsync()
            {
                var query = @"
            query HomepageData {
              trending: Page(page: 1, perPage: 5) {
                media(type: MANGA, countryOfOrigin: KR, sort: TRENDING_DESC, isAdult: false) {
                  id
                  title { romaji english }
                  description(asHtml: false)
                  coverImage { extraLarge large }
                  bannerImage
                  averageScore
                  popularity
                  chapters
                  status
                  updatedAt
                }
              }
              popular: Page(page: 1, perPage: 15) {
                media(type: MANGA, countryOfOrigin: KR, sort: POPULARITY_DESC, isAdult: false) {
                  id
                  title { romaji english }
                  coverImage { large }
                  averageScore
                  popularity
                  updatedAt
                }
              }
              updated: Page(page: 1, perPage: 9) {
                media(type: MANGA, countryOfOrigin: KR, sort: UPDATED_AT_DESC, isAdult: false) {
                  id
                  title { romaji english }
                  coverImage { large }
                  averageScore
                  chapters
                  status
                  updatedAt
                }
              }
           
            }";

                var body = JsonSerializer.Serialize(new { query, variables = new { } });
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await _http.PostAsync("https://graphql.anilist.co", content);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                var data = doc.RootElement.GetProperty("data");

                var randomPage = new Random().Next(1, 20);

                var discoverQuery = @"
query {
  Page(page: " + randomPage + @", perPage: 20) {
    media(type: MANGA, countryOfOrigin: KR, sort: ID_DESC, isAdult: false) {
      id
      title { romaji english }
      description(asHtml: false)
      coverImage { large }
      averageScore
      popularity
    }
  }
}";

                var discoverBody = JsonSerializer.Serialize(new { query = discoverQuery, variables = new { } });
                var discoverContent = new StringContent(discoverBody, Encoding.UTF8, "application/json");
                var discoverResponse = await _http.PostAsync("https://graphql.anilist.co", discoverContent);
                discoverResponse.EnsureSuccessStatusCode();
                var discoverDoc = JsonDocument.Parse(await discoverResponse.Content.ReadAsStringAsync());
                var discoverData = discoverDoc.RootElement.GetProperty("data");
                var randomItems = MapPage(discoverData, "Page").OrderBy(_ => Guid.NewGuid()).Take(6).ToList();



                return new HomeViewModel
                {
                    Trending = MapPage(data, "trending"),
                    Popular = MapPage(data, "popular"),
                    NewlyUpdated = MapPage(data, "updated"),
                    Random = randomItems,
                };
            }

            // ── Maps a page of results to a List<Manhwa> ─────────────
            private static List<Manhwa> MapPage(JsonElement data, string key)
            {
                var list = new List<Manhwa>();
                if (!data.TryGetProperty(key, out var page)) return list;
                if (!page.TryGetProperty("media", out var media)) return list;
                foreach (var m in media.EnumerateArray())
                    list.Add(MapManhwa(m));
                return list;
            }

            // ── Maps a single JSON node to a Manhwa object ────────────
            private static Manhwa MapManhwa(JsonElement m)
            {
                // Title
                var titleNode = m.TryGetProperty("title", out var t) ? t : default;
                var title = GetString(titleNode, "english") ?? GetString(titleNode, "romaji") ?? "Unknown";

                // Cover image
                var coverNode = m.TryGetProperty("coverImage", out var c) ? c : default;
                var cover = GetString(coverNode, "extraLarge") ?? GetString(coverNode, "large") ?? "";

                // Banner image (wide, used as hero background)
                var banner = m.TryGetProperty("bannerImage", out var b)
                             && b.ValueKind == JsonValueKind.String
                             ? b.GetString() ?? "" : "";

                // Score: AniList is 0-100, we convert to 0.0-10.0
                double rating = 0;
                if (m.TryGetProperty("averageScore", out var s) && s.ValueKind == JsonValueKind.Number)
                    rating = Math.Round(s.GetDouble() / 10.0, 1);

                // Popularity
                int popularity = 0;
                if (m.TryGetProperty("popularity", out var p) && p.ValueKind == JsonValueKind.Number)
                    popularity = p.GetInt32();

                // Last updated (Unix timestamp → DateTime)
                var updated = DateTime.UtcNow;
                if (m.TryGetProperty("updatedAt", out var ua) && ua.ValueKind == JsonValueKind.Number)
                    updated = DateTimeOffset.FromUnixTimeSeconds(ua.GetInt64()).UtcDateTime;

                // Chapter count
                string? chapter = null;
                if (m.TryGetProperty("chapters", out var ch) && ch.ValueKind == JsonValueKind.Number)
                    chapter = $"Ch. {ch.GetInt32()}";

                // Status
                var status = m.TryGetProperty("status", out var st)
                             && st.ValueKind == JsonValueKind.String
                             ? st.GetString() ?? "" : "";

                // Description (strip HTML tags)
                var desc = m.TryGetProperty("description", out var d)
                           && d.ValueKind == JsonValueKind.String
                           ? StripHtml(d.GetString() ?? "") : "";

                // ID
                int id = m.TryGetProperty("id", out var idEl)
                         && idEl.ValueKind == JsonValueKind.Number
                         ? idEl.GetInt32() : 0;

                return new Manhwa
                {
                    Id = id,
                    Title = title,
                    Description = desc,
                    CoverImageUrl = cover,
                    BannerImageUrl = banner,
                    Rating = rating,
                    Popularity = popularity,
                    LastUpdated = updated,
                    LatestChapter = chapter,
                    Status = status,
                };
            }

            // ── Helpers ───────────────────────────────────────────────
            private static string? GetString(JsonElement el, string key)
            {
                if (el.ValueKind == JsonValueKind.Undefined) return null;
                if (!el.TryGetProperty(key, out var v)) return null;
                return v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            }

            private static string StripHtml(string input)
            {
                var plain = Regex.Replace(input, "<.*?>", " ");
                plain = Regex.Replace(plain, @"\s{2,}", " ").Trim();
                return plain.Length > 300 ? plain[..300] + "…" : plain;
            }
        }
    }
}

