using Microsoft.Playwright.NUnit;
using Microsoft.Playwright;

namespace ManwhaWebsite.Tests;

[TestFixture]
public class ScreenshotTests : PageTest
{
    [Test]
    public async Task CaptureAllHeroSlides()
    {
        await Page.SetViewportSizeAsync(1400, 800);
        await Page.GotoAsync("http://localhost:5000");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var slideCount = await Page.Locator(".hero-slide").CountAsync();
        for (int i = 0; i < slideCount; i++)
        {
            await Page.EvaluateAsync($"() => goToSlide({i})");
            await Page.WaitForTimeoutAsync(1000);
            await Page.Locator(".hero").ScreenshotAsync(new LocatorScreenshotOptions
            {
                Path = $"C:/Users/akaru/Desktop/slide_{i}.png"
            });
        }
        Assert.Pass($"Captured {slideCount} slides.");
    }
}
