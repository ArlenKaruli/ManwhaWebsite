using Microsoft.Playwright.NUnit;
using Microsoft.Playwright;
namespace ManwhaWebsite.Tests;
[TestFixture]
public class MobileScreenshots : PageTest
{
    [Test]
    public async Task CaptureMobileBreakpoints()
    {
        foreach (var (w, h, name) in new[]{(360,780,"360"),(480,850,"480"),(768,1024,"768")})
        {
            await Page.SetViewportSizeAsync(w, h);
            await Page.GotoAsync("http://localhost:5000");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Page.ScreenshotAsync(new PageScreenshotOptions { Path = $"C:/Users/akaru/Desktop/mobile_{name}.png", FullPage = false });
        }
        Assert.Pass();
    }
}
