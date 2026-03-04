using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShipmentTrackerMcp;

var builder = Host.CreateApplicationBuilder(args);

// MCP uses stdout for JSON-RPC — any other output breaks the protocol.
// Clear the default stdout logger and replace it with one that writes to stderr.
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddSingleton<SchenkerClient>();

builder.Services
    .AddMcpServer(_ => { })

    // MCP clients communicate over stdin/stdout.
    .WithStdioServerTransport()

    // Scans ShipmentTrackingTool for [McpServerTool]-annotated methods and registers them.
    .WithTools<ShipmentTrackingTool>();

await builder.Build().RunAsync();
