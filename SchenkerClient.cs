using Microsoft.Playwright;

namespace ShipmentTrackerMcp;

public class SchenkerClient
{
    // The page that triggers the underlying API calls when loaded
    private const string TrackingPageUrl = "https://www.dbschenker.com/app/tracking-public/";

    // The detail API URL contains a path segment after /shipments/ (e.g. /shipments/land/LandStt:...)
    // The search API URL uses a query parameter instead (e.g. /shipments?query=...)
    // The trailing slash makes them unambiguous without extra checks
    private const string DetailApiUrlFragment = "/tracking-public/shipments/";

    private const int TimeoutMs = 60_000;

    public async Task<string> FetchShipmentJsonAsync(string referenceNumber)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            // Suppress the automation flag that sites use to detect headless browsers
            Args = ["--disable-blink-features=AutomationControlled"]
        });

        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36"
        });

        // DEBUG: log every response URL to stderr so we can see what the browser is requesting
        page.Response += (_, response) =>
            Console.Error.WriteLine($"[DEBUG] Response: {response.Status} {response.Url}");

        // Register the listener before navigating — the browser will make the API call
        // as part of loading the page, and we capture the response via network interception
        // Wait specifically for a successful (200) detail response — the page may first receive
        // a 429 (rate limited) and then retry with a valid captcha token
        var detailResponseTask = page.WaitForResponseAsync(
            r => r.Url.Contains(DetailApiUrlFragment) && r.Status == 200,
            new PageWaitForResponseOptions { Timeout = TimeoutMs });

        await page.GotoAsync(
            $"{TrackingPageUrl}?refNumber={referenceNumber}&language_region=en-US_US&uiMode=");

        var detailResponse = await detailResponseTask;
        return await detailResponse.TextAsync();
    }
}
