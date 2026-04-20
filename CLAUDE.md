# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Run the web app (starts at http://localhost:5000)
dotnet run --project ManwhaWebsite/ManwhaWebsite.csproj --urls http://localhost:5000

# Build entire solution
dotnet build

# Run all tests (requires app running at http://localhost:5000 first)
dotnet test ManwhaWebsite.Tests/ManwhaWebsite.Tests.csproj

# Run a single test by name
dotnet test ManwhaWebsite.Tests/ManwhaWebsite.Tests.csproj --filter "TestClassName.MethodName"

# Capture hero slide screenshots to Desktop (visual regression)
dotnet test ManwhaWebsite.Tests/ManwhaWebsite.Tests.csproj --filter "CaptureAllHeroSlides"

# Apply pending EF Core migrations
dotnet ef database update --project ManwhaWebsite/ManwhaWebsite.csproj

# Add a new migration
dotnet ef migrations add MigrationName --project ManwhaWebsite/ManwhaWebsite.csproj

# Install Playwright browsers (required once after cloning the test project)
playwright install chromium
```

## Architecture

**Two projects in solution:**
- `ManwhaWebsite/` — ASP.NET Core 9 MVC web app
- `ManwhaWebsite.Tests/` — NUnit + Playwright E2E tests (assumes app running on `http://localhost:5000`)

**Homepage data flow:**
`HomeController.Index()` → `AniListService.GetHomepageDataAsync()` → two sequential GraphQL POSTs to `https://graphql.anilist.co` → `HomeViewModel` → `Views/Home/Index.cshtml`

The first request fetches trending (5), popular (15), and updated (9) lists. The second fetches a random page (1–20) of 20 titles for the Discover section, shuffled client-side.

**Homepage view is self-contained:** `Views/Home/Index.cshtml` renders as a full HTML document with its own `<!DOCTYPE>`, `<head>`, and `<body>`. It does **not** use `_Layout.cshtml`. All CSS lives in `wwwroot/css/home.css` (linked via `<link asp-append-version="true">`); all JS is inline at the bottom of the view.

**Database:** SQL Server LocalDB via EF Core. `ApplicationDbContext` inherits `IdentityDbContext`. On startup, if `Manhwas` is empty the app seeds 15 example titles. Schema is defined by three migrations in `Data/Migrations/`.

**AniList score mapping:** API returns 0–100; `AniListService` divides by 10 and rounds to 1 decimal.

**Razor escaping in `.cshtml`:** `@` must be escaped as `@@` inside Razor views — but in plain `.css` files write `@keyframes` and `@media` normally.

**E2E tests:** `HomepageTests.cs` uses `PageTest` from `Microsoft.Playwright.NUnit`. The `CaptureAllHeroSlides` test in `ScreenshotTest.cs` saves `slide_N.png` files to `C:/Users/akaru/Desktop/` and is useful for visual regression after UI changes.
