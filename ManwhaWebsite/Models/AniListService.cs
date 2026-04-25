namespace ManwhaWebsite.Models
{
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Caching.Memory;

    namespace ManhwaVault.Services
    {
        public class AniListService
        {
            private readonly HttpClient _http;
            private readonly IMemoryCache _cache;
            private const string CacheKey = "homepage_data";

            public AniListService(HttpClient http, IMemoryCache cache)
            {
                _http = http;
                _cache = cache;
            }

            public async Task<HomeViewModel> GetHomepageDataAsync()
            {
                if (_cache.TryGetValue(CacheKey, out HomeViewModel? cached) && cached is not null)
                    return cached;

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
              popular: Page(page: 1, perPage: 20) {
                media(type: MANGA, countryOfOrigin: KR, sort: POPULARITY_DESC, isAdult: false) {
                  id
                  title { romaji english }
                  coverImage { large }
                  averageScore
                  popularity
                  updatedAt
                }
              }
              updated: Page(page: 1, perPage: 20) {
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

                var randomPage = new Random().Next(1, 11);

                var discoverQuery = @"
query {
  Page(page: " + randomPage + @", perPage: 20) {
    media(type: MANGA, countryOfOrigin: KR, sort: POPULARITY_DESC, averageScore_greater: 0, isAdult: false) {
      id
      title { romaji english }
      description(asHtml: false)
      coverImage { large }
      averageScore
      popularity
      status
      chapters
      genres
    }
  }
}";

                var discoverBody = JsonSerializer.Serialize(new { query = discoverQuery, variables = new { } });
                var discoverContent = new StringContent(discoverBody, Encoding.UTF8, "application/json");
                var discoverResponse = await _http.PostAsync("https://graphql.anilist.co", discoverContent);
                discoverResponse.EnsureSuccessStatusCode();
                var discoverDoc = JsonDocument.Parse(await discoverResponse.Content.ReadAsStringAsync());
                var discoverData = discoverDoc.RootElement.GetProperty("data");
                var randomItems = MapPage(discoverData, "Page").OrderBy(_ => Guid.NewGuid()).Take(12).ToList();



                var result = new HomeViewModel
                {
                    Trending = MapPage(data, "trending"),
                    Popular = MapPage(data, "popular"),
                    NewlyUpdated = MapPage(data, "updated"),
                    Random = randomItems,
                };
                _cache.Set(CacheKey, result, TimeSpan.FromMinutes(20));
                return result;
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
                int? chapterCount = null;
                if (m.TryGetProperty("chapters", out var ch) && ch.ValueKind == JsonValueKind.Number)
                {
                    chapterCount = ch.GetInt32();
                    chapter = $"Ch. {chapterCount}";
                }

                // Genres
                var genres = new List<string>();
                if (m.TryGetProperty("genres", out var g) && g.ValueKind == JsonValueKind.Array)
                    foreach (var genre in g.EnumerateArray())
                        if (genre.ValueKind == JsonValueKind.String && genre.GetString() is string gs)
                            genres.Add(gs);

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
                    ChapterCount = chapterCount,
                    Status = status,
                    Genres = genres,
                };
            }

            public async Task<MangaDetailViewModel?> GetMangaDetailAsync(int id)
            {
                var cacheKey = $"manga_detail_{id}";
                if (_cache.TryGetValue(cacheKey, out MangaDetailViewModel? cached) && cached is not null)
                    return cached;

                var query = @"
query MangaDetail($id: Int) {
  Media(id: $id, type: MANGA) {
    id
    title { romaji english }
    description(asHtml: false)
    coverImage { extraLarge large }
    bannerImage
    averageScore
    popularity
    favourites
    status
    chapters
    volumes
    genres
    startDate { year }
    rankings { rank type allTime }
    characters(page: 1, perPage: 12, sort: ROLE) {
      nodes { id name { full } image { large } }
    }
  }
}";
                var body = JsonSerializer.Serialize(new { query, variables = new { id } });
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await _http.PostAsync("https://graphql.anilist.co", content);
                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("data", out var data)) return null;
                if (!data.TryGetProperty("Media", out var m) || m.ValueKind == JsonValueKind.Null) return null;

                var titleNode = m.TryGetProperty("title", out var t) ? t : default;
                var english = GetString(titleNode, "english");
                var romaji = GetString(titleNode, "romaji");
                var title = english ?? romaji ?? "Unknown";
                var altTitle = english != null && romaji != null && romaji != english ? romaji : null;

                var coverNode = m.TryGetProperty("coverImage", out var c) ? c : default;
                var cover = GetString(coverNode, "extraLarge") ?? GetString(coverNode, "large") ?? "";
                var banner = m.TryGetProperty("bannerImage", out var b) && b.ValueKind == JsonValueKind.String
                    ? b.GetString() ?? "" : "";

                double score = 0;
                if (m.TryGetProperty("averageScore", out var s) && s.ValueKind == JsonValueKind.Number)
                    score = Math.Round(s.GetDouble() / 10.0, 1);

                int popularity = 0;
                if (m.TryGetProperty("popularity", out var p) && p.ValueKind == JsonValueKind.Number)
                    popularity = p.GetInt32();

                int favourites = 0;
                if (m.TryGetProperty("favourites", out var fav) && fav.ValueKind == JsonValueKind.Number)
                    favourites = fav.GetInt32();

                var status = m.TryGetProperty("status", out var st) && st.ValueKind == JsonValueKind.String
                    ? st.GetString() ?? "" : "";

                int? chapters = null;
                if (m.TryGetProperty("chapters", out var ch) && ch.ValueKind == JsonValueKind.Number)
                    chapters = ch.GetInt32();

                int? volumes = null;
                if (m.TryGetProperty("volumes", out var vol) && vol.ValueKind == JsonValueKind.Number)
                    volumes = vol.GetInt32();

                int? startYear = null;
                if (m.TryGetProperty("startDate", out var sd) && sd.TryGetProperty("year", out var yr)
                    && yr.ValueKind == JsonValueKind.Number)
                    startYear = yr.GetInt32();

                var desc = m.TryGetProperty("description", out var d) && d.ValueKind == JsonValueKind.String
                    ? StripHtmlFull(d.GetString() ?? "") : "";

                var genres = new List<string>();
                if (m.TryGetProperty("genres", out var g) && g.ValueKind == JsonValueKind.Array)
                    foreach (var genre in g.EnumerateArray())
                        if (genre.ValueKind == JsonValueKind.String && genre.GetString() is string gs)
                            genres.Add(gs);

                int scoreRank = 0, popularityRank = 0;
                if (m.TryGetProperty("rankings", out var rankings) && rankings.ValueKind == JsonValueKind.Array)
                {
                    foreach (var r in rankings.EnumerateArray())
                    {
                        var allTime = r.TryGetProperty("allTime", out var at) && at.ValueKind == JsonValueKind.True;
                        if (!allTime) continue;
                        var rType = r.TryGetProperty("type", out var rt) && rt.ValueKind == JsonValueKind.String
                            ? rt.GetString() : "";
                        var rank = r.TryGetProperty("rank", out var rk) && rk.ValueKind == JsonValueKind.Number
                            ? rk.GetInt32() : 0;
                        if (rType == "RATED" && scoreRank == 0) scoreRank = rank;
                        if (rType == "POPULAR" && popularityRank == 0) popularityRank = rank;
                    }
                }

                var characters = new List<CharacterInfo>();
                if (m.TryGetProperty("characters", out var chars) && chars.TryGetProperty("nodes", out var nodes)
                    && nodes.ValueKind == JsonValueKind.Array)
                {
                    foreach (var node in nodes.EnumerateArray())
                    {
                        var nameNode = node.TryGetProperty("name", out var n) ? n : default;
                        var fullName = GetString(nameNode, "full") ?? "Unknown";
                        var imgNode = node.TryGetProperty("image", out var img) ? img : default;
                        var imgUrl = GetString(imgNode, "large") ?? "";
                        characters.Add(new CharacterInfo { Name = fullName, ImageUrl = imgUrl });
                    }
                }

                var vm = new MangaDetailViewModel
                {
                    AniListId = id,
                    Title = title,
                    AlternativeTitle = altTitle,
                    Description = desc,
                    CoverImageUrl = cover,
                    BannerImageUrl = string.IsNullOrEmpty(banner) ? null : banner,
                    Score = score,
                    ScoreRank = scoreRank,
                    PopularityRank = popularityRank,
                    Popularity = popularity,
                    Favourites = favourites,
                    Status = status,
                    Chapters = chapters,
                    Volumes = volumes,
                    StartYear = startYear,
                    Genres = genres,
                    Characters = characters,
                };

                _cache.Set(cacheKey, vm, TimeSpan.FromMinutes(30));
                return vm;
            }

            public async Task<List<Manhwa>> GetDiscoverAsync()
            {
                var randomPage = new Random().Next(1, 11);
                var gql = @"
query {
  Page(page: " + randomPage + @", perPage: 20) {
    media(type: MANGA, countryOfOrigin: KR, sort: POPULARITY_DESC, averageScore_greater: 0, isAdult: false) {
      id
      title { romaji english }
      description(asHtml: false)
      coverImage { large }
      averageScore
      popularity
      status
      chapters
      genres
    }
  }
}";
                var body = JsonSerializer.Serialize(new { query = gql, variables = new { } });
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await _http.PostAsync("https://graphql.anilist.co", content);
                if (!response.IsSuccessStatusCode) return new List<Manhwa>();
                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("data", out var data)) return new List<Manhwa>();
                return MapPage(data, "Page").OrderBy(_ => Guid.NewGuid()).Take(20).ToList();
            }

            public async Task<List<Manhwa>> BrowseAsync(
                string? search,
                List<string>? genres,
                string? status,
                double? minRating,
                int? minChapters,
                int? maxChapters,
                int? publishedBefore,
                int? publishedAfter,
                int page = 1)
            {
                var filters = new List<string> { "type: MANGA", "countryOfOrigin: KR", "isAdult: false" };
                bool hasSearch = !string.IsNullOrWhiteSpace(search);

                if (hasSearch)
                    filters.Add("search: $search");

                var allowedGenres = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "Action", "Adventure", "Comedy", "Drama", "Fantasy", "Horror",
                    "Mystery", "Psychological", "Romance", "Sci-Fi", "Slice of Life",
                    "Sports", "Supernatural", "Thriller"
                };
                var safeGenres = genres?.Where(g => allowedGenres.Contains(g)).ToList();
                if (safeGenres?.Count > 0)
                    filters.Add($"genre_in: [{string.Join(", ", safeGenres.Select(g => $"\"{g}\""))}]");

                var allowedStatuses = new HashSet<string> { "FINISHED", "RELEASING", "NOT_YET_RELEASED", "HIATUS", "CANCELLED" };
                if (!string.IsNullOrWhiteSpace(status) && allowedStatuses.Contains(status.ToUpper()))
                    filters.Add($"status: {status.ToUpper()}");

                if (minRating > 0)
                    filters.Add($"averageScore_greater: {(int)(minRating!.Value * 10)}");

                if (minChapters > 0)
                    filters.Add($"chapters_greater: {minChapters!.Value - 1}");

                if (maxChapters > 0)
                    filters.Add($"chapters_lesser: {maxChapters!.Value + 1}");

                if (publishedAfter > 0)
                    filters.Add($"startDate_greater: {publishedAfter!.Value * 10000}");

                if (publishedBefore > 0)
                    filters.Add($"startDate_lesser: {publishedBefore!.Value * 10000 + 1231}");

                filters.Add(hasSearch ? "sort: SEARCH_MATCH" : "sort: POPULARITY_DESC");

                var varDecl = hasSearch ? "($search: String)" : "";
                var gql = $@"
query Browse{varDecl} {{
  Page(page: {page}, perPage: 40) {{
    media({string.Join(", ", filters)}) {{
      id
      title {{ romaji english }}
      coverImage {{ large }}
      averageScore
      status
      chapters
      genres
    }}
  }}
}}";

                var variables = hasSearch ? (object)new { search = search!.Trim() } : new { };
                var body = JsonSerializer.Serialize(new { query = gql, variables });
                var reqContent = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await _http.PostAsync("https://graphql.anilist.co", reqContent);
                if (!response.IsSuccessStatusCode) return new List<Manhwa>();
                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("data", out var data)) return new List<Manhwa>();
                return MapPage(data, "Page");
            }

            public async Task<List<Manhwa>> SearchAsync(string query, int count = 8)
            {
                var gql = @"
query Search($search: String, $perPage: Int) {
  Page(page: 1, perPage: $perPage) {
    media(type: MANGA, countryOfOrigin: KR, search: $search, isAdult: false, sort: SEARCH_MATCH) {
      id
      title { romaji english }
      coverImage { large }
      averageScore
      status
      chapters
    }
  }
}";
                var body = JsonSerializer.Serialize(new { query = gql, variables = new { search = query, perPage = count } });
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await _http.PostAsync("https://graphql.anilist.co", content);
                if (!response.IsSuccessStatusCode) return new List<Manhwa>();
                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("data", out var data)) return new List<Manhwa>();
                return MapPage(data, "Page");
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

            private static string StripHtmlFull(string input)
            {
                var plain = Regex.Replace(input, "<.*?>", " ");
                return Regex.Replace(plain, @"\s{2,}", " ").Trim();
            }
        }
    }
}

