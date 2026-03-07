using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace ShipmentTrackerMcp;

[McpServerToolType]
public class ShipmentTrackingTool(SchenkerClient schenkerClient)
{
    private static readonly JsonSerializerOptions SerializeOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // [Description] attributes are exposed to the MCP client as the tool/parameter schema,
    // so the AI knows what the tool does and what input it expects
    [McpServerTool]
    [Description("Tracks a DB Schenker shipment and returns structured information including goods details, tracking history, and per-package events.")]
    public async Task<string> TrackShipment(
        [Description("The DB Schenker shipment reference number (e.g. 1806290829)")]
        string referenceNumber)
    {
        referenceNumber = referenceNumber.Trim();

        if (string.IsNullOrEmpty(referenceNumber))
            return "Error: Reference number cannot be empty.";

        try
        {
            var result = await schenkerClient.FetchShipmentAsync(referenceNumber);
            return JsonSerializer.Serialize(result, SerializeOptions);
        }
        catch (InvalidOperationException ex)
        {
            return $"Error: {ex.Message}";
        }
        catch (TimeoutException)
        {
            return "Error: The DB Schenker website did not respond in time. Please try again.";
        }
        catch (Exception ex)
        {
            return $"Error: An unexpected error occurred — {ex.Message}";
        }
    }
}
