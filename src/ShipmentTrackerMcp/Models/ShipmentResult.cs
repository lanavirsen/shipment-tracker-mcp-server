namespace ShipmentTrackerMcp.Models;

// The structured result returned to the MCP client after fetching and mapping the shipment data.
// Sender/receiver names are not available in the API - only location data is returned.
// Dates are formatted as human-readable strings (e.g. "2025-12-18 10:11 UTC") rather than raw ISO 8601.

public record ShipmentResult(
    string ReferenceNumber,
    string SttNumber,
    string TransportMode,
    string? Product,
    string Status,
    PlaceInfo Sender,
    PlaceInfo Receiver,
    GoodsInfo Goods,
    DeliveryInfo DeliveryDates,
    List<TrackingEvent> TrackingHistory,
    List<PackageInfo> Packages,
    ReferencesInfo References
);

public record PlaceInfo(string City, string Country, string? PostCode);

public record GoodsInfo(
    int Pieces,
    string? Weight,
    string? Volume,
    string? LoadingMeters,
    bool? Stackable
);

public record DeliveryInfo(string? Estimated, string? Agreed);

public record TrackingEvent(
    string Code,
    string Date,
    string? Location,
    string? Country,
    string? Comment
);

public record PackageInfo(string Id, List<PackageEvent> Events);

public record PackageEvent(string Code, string Date, string? Location, string? Country);

public record ReferencesInfo(
    List<string> ShipperReferences,
    List<string> ConsigneeReferences,
    List<string> WaybillNumbers
);
