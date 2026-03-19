using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ShipmentTrackerMcp;

internal static class CaptchaSolver
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Solves a proof-of-work CAPTCHA challenge.
    /// Takes the raw value of the Captcha-Puzzle response header and returns
    /// the value to send as the Captcha-Solution request header on retry.
    /// </summary>
    public static string Solve(string captchaPuzzleHeader)
    {
        // Header value is base64 of a comma-separated list of JWT strings.
        // Each JWT payload contains a puzzle field (base64-encoded binary blob).
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(captchaPuzzleHeader));
        var results = decoded.Split(',').Select(jwt =>
        {
            var payloadJson = Encoding.UTF8.GetString(FromBase64Url(jwt.Split('.')[1]));
            var payload = JsonSerializer.Deserialize<PuzzlePayload>(payloadJson, JsonOptions)!;
            var puzzle = Convert.FromBase64String(payload.Puzzle);
            return new SolvedPuzzle(Jwt: jwt, Solution: SolvePuzzle(puzzle));
        });

        return Convert.ToBase64String(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(results, JsonOptions)));
    }

    private static string SolvePuzzle(byte[] puzzle)
    {
        // Difficulty is encoded in the puzzle: target = puzzle[14] * 2^(8 * (puzzle[13] - 3))
        // The hash (little-endian BigInteger) must be less than the target.
        var target = new BigInteger(puzzle[14]) * BigInteger.Pow(2, 8 * (puzzle[13] - 3));

        // Hash input: first 32 bytes of the puzzle followed by the 8-byte nonce.
        var hashInput = new byte[40];
        puzzle.AsSpan(0, 32).CopyTo(hashInput);

        for (long nonce = 0; nonce < long.MaxValue; nonce++)
        {
            WriteLittleEndian8(nonce, hashInput, offset: 32);

            // Double SHA-256 interpreted as a little-endian unsigned integer.
            var hash = SHA256.HashData(SHA256.HashData(hashInput));
            if (ToUnsignedLittleEndianBigInteger(hash) < target)
                return Convert.ToBase64String(hashInput[32..]);
        }

        throw new InvalidOperationException("Failed to solve captcha puzzle — nonce space exhausted.");
    }

    private static void WriteLittleEndian8(long value, byte[] destination, int offset)
    {
        for (int i = 0; i < 8; i++, value >>= 8)
            destination[offset + i] = (byte)(value & 0xFF);
    }

    private static BigInteger ToUnsignedLittleEndianBigInteger(byte[] bytes)
    {
        // BigInteger(byte[]) treats input as little-endian. Append 0x00 to ensure
        // the value is interpreted as positive (unsigned).
        var unsigned = new byte[bytes.Length + 1];
        bytes.CopyTo(unsigned, 0);
        return new BigInteger(unsigned);
    }

    private static byte[] FromBase64Url(string base64Url)
    {
        var s = base64Url.Replace('-', '+').Replace('_', '/');
        return (s.Length % 4) switch
        {
            2 => Convert.FromBase64String(s + "=="),
            3 => Convert.FromBase64String(s + "="),
            _ => Convert.FromBase64String(s)
        };
    }

    private record PuzzlePayload
    {
        public string Puzzle { get; init; } = "";
    }

    private record SolvedPuzzle(string Jwt, string Solution);
}
