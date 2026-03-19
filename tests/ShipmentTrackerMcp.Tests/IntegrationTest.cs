using Microsoft.Extensions.Logging.Abstractions;
using ShipmentTrackerMcp;

namespace ShipmentTrackerMcp.Tests;

// Run manually to verify the full HTTP + captcha flow against the live API.
// Not part of the regular test suite — skip in CI by running: dotnet test --filter "Category!=Integration"
public class IntegrationTest
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task FetchShipment_LiveApi_ReturnsResult()
    {
        var http = new HttpClient();
        http.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36");
        http.DefaultRequestHeaders.Add("Accept", "application/json");
        http.DefaultRequestHeaders.Add("Referer", "https://www.dbschenker.com/app/tracking-public/");

        var client = new SchenkerClient(http, NullLogger<SchenkerClient>.Instance);
        var result = await client.FetchShipmentAsync("1806290829");

        Assert.NotNull(result);
        Assert.NotEmpty(result.SttNumber);
        Console.WriteLine($"STT: {result.SttNumber}");
        Console.WriteLine($"Status: {result.Status}");
        Console.WriteLine($"Events: {result.TrackingHistory.Count}");
    }

}
