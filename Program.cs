using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShipmentTrackerMcp;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddMcpServer(_ => { })

    // MCP clients communicate over stdin/stdout.
    .WithStdioServerTransport()

    // Scans ShipmentTrackingTool for [McpServerTool]-annotated methods and registers them.
    .WithTools<ShipmentTrackingTool>();

await builder.Build().RunAsync();
