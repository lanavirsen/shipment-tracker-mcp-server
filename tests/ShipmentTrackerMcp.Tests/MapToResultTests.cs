using ShipmentTrackerMcp.Models;

namespace ShipmentTrackerMcp.Tests;

public class MapToResultTests
{
    // Minimal valid DTO — all optional fields absent, required fields at defaults.
    // Each test copies this and replaces only the fields relevant to what it is testing.
    private static SchenkerShipmentResponse BaseDto() => new()
    {
        SttNumber = "SESRA551153301",
        TransportMode = "LAND",
        Product = "SYSTEM",
        ProgressBar = new SchenkerProgressBar { ActiveStep = "Delivered" },
        Location = new SchenkerLocation(),
        Goods = new SchenkerGoods { Pieces = 1 },
        References = new SchenkerReferences(),
        Events = [],
        Packages = []
    };

    [Fact]
    public void MapsGoods()
    {
        var dto = BaseDto() with
        {
            Goods = new SchenkerGoods
            {
                Pieces = 5,
                Weight = new SchenkerMeasurement { Value = 100.5, Unit = "KG" },
                Volume = new SchenkerMeasurement { Value = 0.8, Unit = "M3" },
                LoadingMeters = new SchenkerMeasurement { Value = 1.2, Unit = "LM" },
                Stackable = true
            }
        };

        var result = SchenkerClient.MapToResult("REF", dto);

        Assert.Equal(5, result.Goods.Pieces);
        Assert.Equal("100.5 KG", result.Goods.Weight);
        Assert.Equal("0.8 M3", result.Goods.Volume);
        Assert.Equal("1.2 LM", result.Goods.LoadingMeters);
        Assert.True(result.Goods.Stackable == true);
    }

    [Fact]
    public void MapsNullMeasurementsToNull()
    {
        var result = SchenkerClient.MapToResult("REF", BaseDto());

        Assert.Null(result.Goods.Weight);
        Assert.Null(result.Goods.Volume);
        Assert.Null(result.Goods.LoadingMeters);
    }

    [Fact]
    public void MapsShipperAndConsigneePlace()
    {
        var dto = BaseDto() with
        {
            Location = new SchenkerLocation
            {
                ShipperPlace = new SchenkerPlace { City = "Berlin", Country = "DE", PostCode = "10115" },
                ConsigneePlace = new SchenkerPlace { City = "Stockholm", Country = "SE", PostCode = "11120" }
            }
        };

        var result = SchenkerClient.MapToResult("REF", dto);

        Assert.Equal(new PlaceInfo("Berlin", "DE", "10115"), result.Sender);
        Assert.Equal(new PlaceInfo("Stockholm", "SE", "11120"), result.Receiver);
    }

    [Fact]
    public void FallsBackToUnknownWhenPlaceIsNull()
    {
        var result = SchenkerClient.MapToResult("REF", BaseDto());

        Assert.Equal(new PlaceInfo("Unknown", "Unknown", null), result.Sender);
        Assert.Equal(new PlaceInfo("Unknown", "Unknown", null), result.Receiver);
    }

    [Fact]
    public void MapsEventsAndPackages()
    {
        var date = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var dto = BaseDto() with
        {
            Events =
            [
                new SchenkerEvent
                {
                    Code = "DLV",
                    Date = date,
                    Location = new SchenkerEventLocation { Name = "Stockholm", CountryCode = "SE" },
                    Comment = "Delivered to door"
                },
                new SchenkerEvent { Code = "PUP", Date = date, Location = null }
            ],
            Packages =
            [
                new SchenkerPackage
                {
                    Id = "PKG-001",
                    Events =
                    [
                        new SchenkerPackageEvent { Code = "ARR", Date = date, Location = "Hamburg", CountryCode = "DE" }
                    ]
                }
            ]
        };

        var result = SchenkerClient.MapToResult("REF", dto);

        Assert.Equal(2, result.TrackingHistory.Count);
        var ev = result.TrackingHistory[0];
        Assert.Equal("DLV", ev.Code);
        Assert.Equal("2026-01-15 10:30 UTC", ev.Date);
        Assert.Equal("Stockholm", ev.Location);
        Assert.Equal("SE", ev.Country);
        Assert.Equal("Delivered to door", ev.Comment);
        Assert.Null(result.TrackingHistory[1].Location);

        Assert.Single(result.Packages);
        var pkg = result.Packages[0];
        Assert.Equal("PKG-001", pkg.Id);
        Assert.Equal("ARR", pkg.Events[0].Code);
        Assert.Equal("2026-01-15 10:30 UTC", pkg.Events[0].Date);
        Assert.Equal("Hamburg", pkg.Events[0].Location);
        Assert.Equal("DE", pkg.Events[0].Country);
    }

    [Fact]
    public void MapsDeliveryDatesWhenBothPresent()
    {
        var dto = BaseDto() with
        {
            DeliveryDate = new SchenkerDeliveryDate
            {
                Estimated = new DateTime(2026, 1, 22, 12, 0, 0, DateTimeKind.Utc),
                Agreed = new DateTime(2026, 1, 21, 9, 0, 0, DateTimeKind.Utc)
            }
        };

        var result = SchenkerClient.MapToResult("REF", dto);

        Assert.Equal("2026-01-22 12:00 UTC", result.DeliveryDates.Estimated);
        Assert.Equal("2026-01-21 09:00 UTC", result.DeliveryDates.Agreed);
    }

    [Fact]
    public void MapsDeliveryDatesWhenOnlyOneIsPresent()
    {
        var dto = BaseDto() with
        {
            DeliveryDate = new SchenkerDeliveryDate
            {
                Estimated = new DateTime(2026, 1, 22, 12, 0, 0, DateTimeKind.Utc),
                Agreed = null
            }
        };

        var result = SchenkerClient.MapToResult("REF", dto);

        Assert.Equal("2026-01-22 12:00 UTC", result.DeliveryDates.Estimated);
        Assert.Null(result.DeliveryDates.Agreed);
    }

    [Fact]
    public void MapsNullDeliveryDatesToNull()
    {
        var result = SchenkerClient.MapToResult("REF", BaseDto());

        Assert.Null(result.DeliveryDates.Estimated);
        Assert.Null(result.DeliveryDates.Agreed);
    }
}
