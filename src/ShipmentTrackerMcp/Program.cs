using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShipmentTrackerMcp;

var builder = Host.CreateApplicationBuilder(args);

// MCP uses stdout for JSON-RPC — any other output breaks the protocol.
// Clear the default stdout logger and replace it with one that writes to stderr.
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddHttpClient<SchenkerClient>(client =>
{
    // Mimic a real browser request so the API doesn't reject us outright.
    client.DefaultRequestHeaders.Add("User-Agent",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("Referer",
        "https://www.dbschenker.com/app/tracking-public/");
});

builder.Services
    .AddMcpServer(_ => { })

    // MCP clients communicate over stdin/stdout.
    .WithStdioServerTransport()

    // Scans ShipmentTrackingTool for [McpServerTool]-annotated methods and registers them.
    .WithTools<ShipmentTrackingTool>();

await builder.Build().RunAsync();
