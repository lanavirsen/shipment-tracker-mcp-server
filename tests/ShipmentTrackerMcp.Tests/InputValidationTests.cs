namespace ShipmentTrackerMcp.Tests;

public class InputValidationTests
{
    // SchenkerClient is null here because input validation happens before any client call.
    // These tests never reach FetchShipmentAsync.
    private readonly ShipmentTrackingTool _tool = new(null!);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyOrWhitespaceReferenceReturnsError(string input)
    {
        var result = await _tool.TrackShipment(input);

        Assert.Equal("Error: Reference number cannot be empty.", result);
    }
}
