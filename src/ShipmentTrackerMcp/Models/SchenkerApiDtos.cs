using System.Text.Json.Serialization;

namespace ShipmentTrackerMcp.Models;

// These types mirror the DB Schenker API response shapes exactly.
// They are internal - only SchenkerClient uses them to deserialize the raw JSON.

// Step 1 search response — used to detect whether the reference number exists
internal record SchenkerSearchResponse
{
    public List<SchenkerSearchResult> Result { get; init; } = [];
}

internal record SchenkerSearchResult
{
    public string Id { get; init; } = "";
    public string TransportMode { get; init; } = "";
}

// Step 2 detail response
internal record SchenkerShipmentResponse
{
    public string SttNumber { get; init; } = "";
    public SchenkerReferences References { get; init; } = new();
    public SchenkerGoods Goods { get; init; } = new();
    public List<SchenkerEvent> Events { get; init; } = [];
    public List<SchenkerPackage> Packages { get; init; } = [];
    public string? Product { get; init; }
    public SchenkerDeliveryDate? DeliveryDate { get; init; }
    public string TransportMode { get; init; } = "";
    public SchenkerProgressBar ProgressBar { get; init; } = new();
    public SchenkerLocation Location { get; init; } = new();
}

internal record SchenkerReferences
{
    public List<string> Shipper { get; init; } = [];
    public List<string> Consignee { get; init; } = [];
    [JsonPropertyName("waybillAndConsignementNumbers")]
    public List<string> WaybillNumbers { get; init; } = [];
}

internal record SchenkerGoods
{
    public int Pieces { get; init; }
    public SchenkerMeasurement? Weight { get; init; }
    public SchenkerMeasurement? Volume { get; init; }
    public SchenkerMeasurement? LoadingMeters { get; init; }
    public bool? Stackable { get; init; }
}

internal record SchenkerMeasurement
{
    public double Value { get; init; }
    public string Unit { get; init; } = "";
}

internal record SchenkerEvent
{
    public string Code { get; init; } = "";
    public DateTime Date { get; init; }
    public SchenkerEventLocation? Location { get; init; }
    public string? Comment { get; init; }
}

internal record SchenkerEventLocation
{
    public string Name { get; init; } = "";
    public string CountryCode { get; init; } = "";
}

internal record SchenkerPackage
{
    public string Id { get; init; } = "";
    public List<SchenkerPackageEvent> Events { get; init; } = [];
}

internal record SchenkerPackageEvent
{
    public string Code { get; init; } = "";
    public string CountryCode { get; init; } = "";
    public string Location { get; init; } = "";
    public DateTime Date { get; init; }
}

internal record SchenkerDeliveryDate
{
    public DateTime? Estimated { get; init; }
    public DateTime? Agreed { get; init; }
}

internal record SchenkerProgressBar
{
    public string ActiveStep { get; init; } = "";
}

internal record SchenkerLocation
{
    public SchenkerPlace? ShipperPlace { get; init; }
    public SchenkerPlace? ConsigneePlace { get; init; }
}

internal record SchenkerPlace
{
    public string City { get; init; } = "";
    public string Country { get; init; } = "";
    public string? PostCode { get; init; }
}
