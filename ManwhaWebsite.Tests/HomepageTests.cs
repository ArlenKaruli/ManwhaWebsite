using Microsoft.Playwright.NUnit;
using Microsoft.Playwright;

namespace ManwhaWebsite.Tests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class HomepageTests : PageTest
{
    private const string BaseUrl = "http://localhost:5000";

    // ── Hero slideshow ────────────────────────────────────────────────────────

    [Test]
    public async Task HeroSlider_FirstSlide_IsVisible()
    {
        await Page.GotoAsync(BaseUrl);
        var firstSlide = Page.Locator(".hero-slide.active").First;
        await Expect(firstSlide).ToBeVisibleAsync();
    }

    [Test]
    public async Task HeroSlider_DotClick_ChangesActiveSlide()
    {
        await Page.GotoAsync(BaseUrl);

        var dots = Page.Locator(".hero-dot");
        var count = await dots.CountAsync();
        if (count < 2) Assert.Inconclusive("Fewer than 2 hero slides — skipping.");

        await dots.Nth(1).ClickAsync();
        await Page.WaitForTimeoutAsync(900); // wait for transition

        var activeDot = Page.Locator(".hero-dot.active");
        await Expect(activeDot).ToHaveCountAsync(1);
        // second dot should now be active
        var activeIndex = await Page.EvaluateAsync<int>(
            "() => [...document.querySelectorAll('.hero-dot')].findIndex(d => d.classList.contains('active'))");
        Assert.That(activeIndex, Is.EqualTo(1));
    }

    [Test]
    public async Task HeroSlider_ThumbClick_ChangesActiveSlide()
    {
        await Page.GotoAsync(BaseUrl);
        await Page.SetViewportSizeAsync(1280, 800); // thumbs visible above 1024px

        var thumbs = Page.Locator(".hero-thumb");
        var count = await thumbs.CountAsync();
        if (count < 2) Assert.Inconclusive("Fewer than 2 hero thumbs.");

        await thumbs.Nth(1).ClickAsync();
        await Page.WaitForTimeoutAsync(900);

        var activeIndex = await Page.EvaluateAsync<int>(
            "() => [...document.querySelectorAll('.hero-thumb')].findIndex(d => d.classList.contains('active'))");
        Assert.That(activeIndex, Is.EqualTo(1));
    }

    [Test]
    public async Task HeroSlider_ArrowKeys_NavigateSlides()
    {
        await Page.GotoAsync(BaseUrl);

        await Page.Keyboard.PressAsync("ArrowRight");
        await Page.WaitForTimeoutAsync(900);

        var activeIndex = await Page.EvaluateAsync<int>(
            "() => [...document.querySelectorAll('.hero-dot')].findIndex(d => d.classList.contains('active'))");
        Assert.That(activeIndex, Is.EqualTo(1));

        await Page.Keyboard.PressAsync("ArrowLeft");
        await Page.WaitForTimeoutAsync(900);

        var backIndex = await Page.EvaluateAsync<int>(
            "() => [...document.querySelectorAll('.hero-dot')].findIndex(d => d.classList.contains('active'))");
        Assert.That(backIndex, Is.EqualTo(0));
    }

    [Test]
    public async Task HeroSlider_AutoAdvances_After5Seconds()
    {
        await Page.GotoAsync(BaseUrl);

        var initialIndex = await Page.EvaluateAsync<int>(
            "() => [...document.querySelectorAll('.hero-dot')].findIndex(d => d.classList.contains('active'))");

        await Page.WaitForTimeoutAsync(6000); // autoplay fires at 5500ms

        var nextIndex = await Page.EvaluateAsync<int>(
            "() => [...document.querySelectorAll('.hero-dot')].findIndex(d => d.classList.contains('active'))");

        Assert.That(nextIndex, Is.Not.EqualTo(initialIndex));
    }

    // ── Nav ──────────────────────────────────────────────────────────────────

    [Test]
    public async Task Nav_Logo_IsVisible()
    {
        await Page.GotoAsync(BaseUrl);
        await Expect(Page.Locator(".nav-logo")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Nav_SignUpButton_IsVisible()
    {
        await Page.GotoAsync(BaseUrl);
        await Expect(Page.Locator(".btn-signup")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Nav_MobileHamburger_OpensDrawer()
    {
        await Page.SetViewportSizeAsync(375, 812);
        await Page.GotoAsync(BaseUrl);

        await Page.Locator(".nav-hamburger").ClickAsync();
        await Expect(Page.Locator(".nav-drawer")).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("open"));
    }

    [Test]
    public async Task Nav_Drawer_ClosesOnBackdropClick()
    {
        await Page.SetViewportSizeAsync(375, 812);
        await Page.GotoAsync(BaseUrl);

        await Page.Locator(".nav-hamburger").ClickAsync();
        // click backdrop (the overlay, not the panel)
        await Page.Locator(".nav-drawer").ClickAsync(new LocatorClickOptions { Position = new Position { X = 350, Y = 400 } });
        await Expect(Page.Locator(".nav-drawer")).Not.ToHaveClassAsync(new System.Text.RegularExpressions.Regex("open"));
    }

    // ── Popular scroll row ────────────────────────────────────────────────────

    [Test]
    public async Task PopularSection_Cards_AreRendered()
    {
        await Page.GotoAsync(BaseUrl);
        var cards = Page.Locator(".section-popular .card");
        var count = await cards.CountAsync();
        Assert.That(count, Is.GreaterThan(0), "Expected at least one popular card.");
    }

    [Test]
    public async Task PopularSection_ScrollRight_MovesRow()
    {
        await Page.GotoAsync(BaseUrl);
        await Page.SetViewportSizeAsync(1280, 800);

        var row = Page.Locator("#popularRow");
        var before = await row.EvaluateAsync<double>("el => el.scrollLeft");

        await Page.Locator(".scroll-btn-right").First.ClickAsync(new LocatorClickOptions { Force = true });
        await Page.WaitForTimeoutAsync(600);

        var after = await row.EvaluateAsync<double>("el => el.scrollLeft");
        Assert.That(after, Is.GreaterThan(before));
    }

    // ── Genre pills ───────────────────────────────────────────────────────────

    [Test]
    public async Task GenrePills_AllPill_IsActiveByDefault()
    {
        await Page.GotoAsync(BaseUrl);
        await Expect(Page.Locator(".genre-pill.active").First).ToContainTextAsync("All");
    }

    // ── Newly Updated grid ────────────────────────────────────────────────────

    [Test]
    public async Task NewlyUpdated_Cards_AreRendered()
    {
        await Page.GotoAsync(BaseUrl);
        var cards = Page.Locator(".updated-card");
        var count = await cards.CountAsync();
        Assert.That(count, Is.GreaterThan(0), "Expected at least one updated card.");
    }

    // ── Discover grid ─────────────────────────────────────────────────────────

    [Test]
    public async Task Discover_Cards_AreRendered()
    {
        await Page.GotoAsync(BaseUrl);
        var cards = Page.Locator(".discover-card");
        var count = await cards.CountAsync();
        Assert.That(count, Is.GreaterThan(0), "Expected at least one discover card.");
    }

    // ── Images ────────────────────────────────────────────────────────────────

    [Test]
    public async Task HeroSlide_CoverFloatImages_LoadWithoutError()
    {
        await Page.GotoAsync(BaseUrl);

        var floats = Page.Locator(".hero-slide-cover-float");
        var count = await floats.CountAsync();
        // Only present when there is no banner — may be 0 if all slides have banners
        for (int i = 0; i < count; i++)
        {
            var naturalWidth = await floats.Nth(i).EvaluateAsync<int>("img => img.naturalWidth");
            Assert.That(naturalWidth, Is.GreaterThan(0), $"Cover float image {i} failed to load.");
        }
    }

    [Test]
    public async Task PopularCards_Images_LoadWithoutError()
    {
        await Page.GotoAsync(BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var imgs = Page.Locator(".section-popular .card-cover img");
        var count = await imgs.CountAsync();
        for (int i = 0; i < count; i++)
        {
            var ok = await imgs.Nth(i).EvaluateAsync<bool>("img => img.complete && img.naturalWidth > 0");
            Assert.That(ok, Is.True, $"Popular card image {i} failed to load.");
        }
    }

    // ── Responsive — no horizontal scroll at each breakpoint ─────────────────

    [Test]
    public async Task Layout_360px_NoHorizontalScroll()
    {
        await Page.SetViewportSizeAsync(360, 780);
        await Page.GotoAsync(BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var scrollWidth = await Page.EvaluateAsync<int>("() => document.body.scrollWidth");
        var clientWidth = await Page.EvaluateAsync<int>("() => document.body.clientWidth");
        Assert.That(scrollWidth, Is.LessThanOrEqualTo(clientWidth + 2));
    }

    [Test]
    public async Task Layout_480px_NoHorizontalScroll()
    {
        await Page.SetViewportSizeAsync(480, 850);
        await Page.GotoAsync(BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var scrollWidth = await Page.EvaluateAsync<int>("() => document.body.scrollWidth");
        var clientWidth = await Page.EvaluateAsync<int>("() => document.body.clientWidth");
        Assert.That(scrollWidth, Is.LessThanOrEqualTo(clientWidth + 2));
    }

    [Test]
    public async Task Layout_768px_NoHorizontalScroll()
    {
        await Page.SetViewportSizeAsync(768, 1024);
        await Page.GotoAsync(BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var scrollWidth = await Page.EvaluateAsync<int>("() => document.body.scrollWidth");
        var clientWidth = await Page.EvaluateAsync<int>("() => document.body.clientWidth");
        Assert.That(scrollWidth, Is.LessThanOrEqualTo(clientWidth + 2));
    }
}
