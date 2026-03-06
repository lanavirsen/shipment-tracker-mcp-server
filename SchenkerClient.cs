using System.Text.Json;
using Microsoft.Playwright;
using ShipmentTrackerMcp.Models;

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

    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<ShipmentResult> FetchShipmentAsync(string referenceNumber)
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
        // as part of loading the page, and we capture the response via network interception.
        // Wait specifically for a successful (200) detail response — the page may first receive
        // a 429 (rate limited) and then retry with a valid captcha token.
        var detailResponseTask = page.WaitForResponseAsync(
            r => r.Url.Contains(DetailApiUrlFragment) && r.Status == 200,
            new PageWaitForResponseOptions { Timeout = TimeoutMs });

        await page.GotoAsync(
            $"{TrackingPageUrl}?refNumber={referenceNumber}&language_region=en-US_US&uiMode=");

        var detailResponse = await detailResponseTask;
        var json = await detailResponse.TextAsync();

        var dto = JsonSerializer.Deserialize<SchenkerShipmentResponse>(json, DeserializeOptions)
            ?? throw new InvalidOperationException("Failed to deserialize shipment response.");

        return MapToResult(referenceNumber, dto);
    }

    // Maps the raw API DTO to the clean ShipmentResult output model.
    private static ShipmentResult MapToResult(string referenceNumber, SchenkerShipmentResponse dto)
    {
        static string? FormatMeasurement(SchenkerMeasurement? m) =>
            m is null ? null : $"{m.Value} {m.Unit}";

        static string FormatDate(DateTime dt) =>
            dt.ToString("yyyy-MM-dd HH:mm UTC");

        var sender = dto.Location.ShipperPlace is { } sp
            ? new PlaceInfo(sp.City, sp.Country, sp.PostCode)
            : new PlaceInfo("Unknown", "Unknown", null);

        var receiver = dto.Location.ConsigneePlace is { } cp
            ? new PlaceInfo(cp.City, cp.Country, cp.PostCode)
            : new PlaceInfo("Unknown", "Unknown", null);

        var goods = new GoodsInfo(
            dto.Goods.Pieces,
            FormatMeasurement(dto.Goods.Weight),
            FormatMeasurement(dto.Goods.Volume),
            FormatMeasurement(dto.Goods.LoadingMeters),
            dto.Goods.Stackable
        );

        var delivery = new DeliveryInfo(
            dto.DeliveryDate?.Estimated is { } e ? FormatDate(e) : null,
            dto.DeliveryDate?.Agreed is { } a ? FormatDate(a) : null
        );

        var trackingHistory = dto.Events
            .Select(ev => new TrackingEvent(
                ev.Code,
                FormatDate(ev.Date),
                ev.Location?.Name,
                ev.Location?.CountryCode,
                ev.Comment))
            .ToList();

        var packages = dto.Packages
            .Select(p => new PackageInfo(
                p.Id,
                p.Events
                    .Select(ev => new PackageEvent(
                        ev.Code,
                        FormatDate(ev.Date),
                        ev.Location,
                        ev.CountryCode))
                    .ToList()))
            .ToList();

        var references = new ReferencesInfo(
            dto.References.Shipper,
            dto.References.Consignee,
            dto.References.WaybillNumbers
        );

        return new ShipmentResult(
            referenceNumber,
            dto.SttNumber,
            dto.TransportMode,
            dto.Product,
            dto.ProgressBar.ActiveStep,
            sender,
            receiver,
            goods,
            delivery,
            trackingHistory,
            packages,
            references
        );
    }
}
