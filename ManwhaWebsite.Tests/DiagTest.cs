using Microsoft.Playwright.NUnit;
using Microsoft.Playwright;

namespace ManwhaWebsite.Tests;

[TestFixture]
public class DiagTests : PageTest
{
    [Test]
    public async Task InspectSlide0()
    {
        await Page.SetViewportSizeAsync(1400, 800);
        await Page.GotoAsync("http://localhost:5000");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Count cover floats on slide 0
        var floatCount = await Page.Locator(".hero-slide.active .hero-slide-cover-float").CountAsync();
        var bgClass = await Page.Locator(".hero-slide.active .hero-slide-bg").GetAttributeAsync("class");
        var bgStyle = await Page.Locator(".hero-slide.active .hero-slide-bg").GetAttributeAsync("style");

        Console.WriteLine($"Cover floats on active slide: {floatCount}");
        Console.WriteLine($"BG class: {bgClass}");
        Console.WriteLine($"BG style: {bgStyle}");

        // Screenshot just the cover-float element if present
        if (floatCount > 0)
        {
            var box = await Page.Locator(".hero-slide.active .hero-slide-cover-float").BoundingBoxAsync();
            Console.WriteLine($"Float bounding box: x={box?.X} y={box?.Y} w={box?.Width} h={box?.Height}");
        }

        Assert.That(floatCount, Is.EqualTo(1), "Expected 1 cover float on active slide.");
    }
}
