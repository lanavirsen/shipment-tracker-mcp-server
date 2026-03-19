# Shipment Tracker MCP Server

An MCP (Model Context Protocol) server that tracks DB Schenker shipments. Exposes a single `track_shipment` tool that accepts a DB Schenker reference number and returns structured shipment data including goods details, tracking history, and per-package events.

## How it works

The DB Schenker tracking API protects its endpoints with a proof-of-work CAPTCHA. Requests without a valid solution are rejected with 429. The server handles this by:

1. Making the API request and receiving a 429 with a `Captcha-Puzzle` response header
2. Decoding the puzzle challenges from the header (base64-encoded JWTs, each containing a binary puzzle blob)
3. Solving each puzzle by finding a nonce where `SHA256(SHA256(puzzle_bytes | nonce))` meets a difficulty target encoded in the puzzle itself
4. Retrying the request with the solutions in the `Captcha-Solution` header

This runs entirely in-process with no browser dependency - the proof-of-work algorithm was reverse-engineered from the site's JavaScript.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)

## Setup

**1. Clone the repository and check out this branch**

```bash
git clone <repo-url>
cd shipment-tracker-mcp-server
git checkout captcha-pow-direct-http
```

**2. Build**

```bash
dotnet build --configuration Release src/ShipmentTrackerMcp
```

## Running the server

```bash
dotnet run --no-build --configuration Release --project src/ShipmentTrackerMcp
```

The server communicates over stdin/stdout using the MCP protocol. In normal use it is launched automatically by an MCP client (see below).

## Connecting to an MCP client

This server implements the standard MCP protocol and works with any MCP-compatible client. Below is an example configuration for Claude Desktop (`claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "shipment-tracker": {
      "command": "dotnet",
      "args": [
        "run",
        "--no-build",
        "--configuration", "Release",
        "--project", "C:\\path\\to\\shipment-tracker-mcp-server\\src\\ShipmentTrackerMcp"
      ]
    }
  }
}
```

Replace `C:\\path\\to\\` with the actual path on your machine. Note that backslashes must be doubled in JSON.

After updating the config, restart Claude Desktop. You can then ask Claude to track a shipment:

> *"Track DB Schenker shipment 1806290829"*

## Testing the tool

**End-to-end via MCP client**

Once connected, ask Claude to track a real shipment:

> *"Track DB Schenker shipment 1806290829"*

You should receive structured data including goods details, tracking history, and per-package events.

To verify error handling, use an invalid reference such as `0000000000` - the tool returns a clear "not found" error rather than timing out.


**Automated tests**

```bash
# Unit tests only (no network required)
dotnet test --filter "Category!=Integration"

# Include live API integration test
dotnet test
```

## Known limitations

- **No sender/receiver names** - the API returns location data only (city, postcode, country). Names are not available.
- **CAPTCHA stability** - the proof-of-work algorithm is reverse-engineered from minified JavaScript. If the site changes its CAPTCHA mechanism, the solver will need to be updated.
- **stdio transport** - the server runs as a local process on the host machine. A production version would use HTTP-based transport, allowing it to be deployed as a shared service and accessed by multiple clients without local installation.
