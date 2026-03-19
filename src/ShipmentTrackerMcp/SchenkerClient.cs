using System.Net;
using System.Text.Json;
using ShipmentTrackerMcp.Models;

namespace ShipmentTrackerMcp;

public class SchenkerClient(HttpClient http)
{
    private const string BaseApiUrl = "https://www.dbschenker.com/nges-portal/api/public/tracking-public";

    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<ShipmentResult> FetchShipmentAsync(string referenceNumber)
    {
        // Step 1: resolve reference number to an internal shipment ID
        var searchResponse = await http.GetAsync($"{BaseApiUrl}/shipments?query={referenceNumber}");

        if (searchResponse.StatusCode == HttpStatusCode.TooManyRequests)
        {
            var hasPuzzle = searchResponse.Headers.Contains("Captcha-Puzzle");
            throw new InvalidOperationException(
                $"Rate limited by search API. Captcha-Puzzle header present: {hasPuzzle}.");
        }

        searchResponse.EnsureSuccessStatusCode();

        var searchJson = await searchResponse.Content.ReadAsStringAsync();
        var searchResult = JsonSerializer.Deserialize<SchenkerSearchResponse>(searchJson, DeserializeOptions);

        if (searchResult?.Result is null || searchResult.Result.Count == 0)
            throw new InvalidOperationException($"No shipment found for reference number '{referenceNumber}'.");

        var match = searchResult.Result[0];
        var mode = match.TransportMode.ToLowerInvariant();

        // Step 2: fetch full shipment details using the internal ID and transport mode
        var detailResponse = await http.GetAsync(
            $"{BaseApiUrl}/shipments/{mode}/{Uri.EscapeDataString(match.Id)}");

        if (detailResponse.StatusCode == HttpStatusCode.TooManyRequests)
        {
            var hasPuzzle = detailResponse.Headers.Contains("Captcha-Puzzle");
            throw new InvalidOperationException(
                $"Rate limited by detail API. Captcha-Puzzle header present: {hasPuzzle}.");
        }

        detailResponse.EnsureSuccessStatusCode();

        var json = await detailResponse.Content.ReadAsStringAsync();
        var dto = JsonSerializer.Deserialize<SchenkerShipmentResponse>(json, DeserializeOptions)
            ?? throw new InvalidOperationException("Failed to deserialize shipment response.");

        return MapToResult(referenceNumber, dto);
    }

    // Maps the raw API DTO to the clean ShipmentResult output model.
    internal static ShipmentResult MapToResult(string referenceNumber, SchenkerShipmentResponse dto)
    {
        static string? FormatMeasurement(SchenkerMeasurement? m) =>
            m is null ? null : $"{m.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)} {m.Unit}";

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
