using System.ComponentModel;
using ModelContextProtocol.Server;

namespace ShipmentTrackerMcp;

[McpServerToolType]
public class ShipmentTrackingTool
{
    // [Description] attributes are exposed to the MCP client as the tool/parameter schema,
    // so the AI knows what the tool does and what input it expects
    [McpServerTool]
    [Description("Tracks a DB Schenker shipment and returns structured information including goods details, tracking history, and per-package events.")]
    public Task<string> TrackShipment(
        [Description("The DB Schenker shipment reference number (e.g. 1806290829)")]
        string referenceNumber)
    {
        throw new NotImplementedException("Tracking not yet implemented.");
    }
}
