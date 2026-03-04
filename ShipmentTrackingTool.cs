using System.ComponentModel;
using ModelContextProtocol.Server;

namespace ShipmentTrackerMcp;

[McpServerToolType]
public class ShipmentTrackingTool(SchenkerClient schenkerClient)
{
    // [Description] attributes are exposed to the MCP client as the tool/parameter schema,
    // so the AI knows what the tool does and what input it expects
    [McpServerTool]
    [Description("Tracks a DB Schenker shipment and returns structured information including goods details, tracking history, and per-package events.")]
    public async Task<string> TrackShipment(
        [Description("The DB Schenker shipment reference number (e.g. 1806290829)")]
        string referenceNumber)
    {
        return await schenkerClient.FetchShipmentJsonAsync(referenceNumber);
    }
}
