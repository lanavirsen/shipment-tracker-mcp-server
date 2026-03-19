using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ShipmentTrackerMcp;

namespace ShipmentTrackerMcp.Tests;

public class CaptchaSolverTests
{
    // Real puzzle bytes captured from the live API.
    // puzzle[13] = 0x21 (33), puzzle[14] = 0x0a (10)
    // → target = 10 * 2^240, expect a solution in the low thousands of iterations.
    private const string KnownPuzzleBase64 =
        "AAAAAAChiGzPW7iw7SEKAAAAAAAAAAAAAAAAAAAAAACDeofcZckXuXmIXak0/MlLpxpEtS+EJzzwhNGRNm5SnA==";

    [Fact]
    public void Solve_ReturnsSolutionThatSatisfiesProofOfWork()
    {
        var header = BuildCaptchaPuzzleHeader(KnownPuzzleBase64);

        var result = CaptchaSolver.Solve(header);

        var items = JsonSerializer.Deserialize<SolvedItem[]>(
            Encoding.UTF8.GetString(Convert.FromBase64String(result)),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        Assert.Single(items);

        var puzzle = Convert.FromBase64String(KnownPuzzleBase64);
        var nonce = Convert.FromBase64String(items[0].Solution);

        Assert.Equal(8, nonce.Length);
        Assert.True(SatisfiesProofOfWork(puzzle, nonce),
            "Solution nonce does not satisfy the proof-of-work condition.");
    }

    [Fact]
    public void Solve_PreservesOriginalJwtInOutput()
    {
        var jwt = BuildJwt(KnownPuzzleBase64);
        var header = Convert.ToBase64String(Encoding.UTF8.GetBytes(jwt));

        var result = CaptchaSolver.Solve(header);

        var items = JsonSerializer.Deserialize<SolvedItem[]>(
            Encoding.UTF8.GetString(Convert.FromBase64String(result)),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        Assert.Equal(jwt, items[0].Jwt);
    }

    // Verifies that SHA256(SHA256(puzzle[0..31] | nonce)) as a little-endian
    // unsigned BigInteger is less than the difficulty target in the puzzle.
    private static bool SatisfiesProofOfWork(byte[] puzzle, byte[] nonce)
    {
        var hashInput = new byte[40];
        puzzle.AsSpan(0, 32).CopyTo(hashInput);
        nonce.CopyTo(hashInput, 32);

        var hash = SHA256.HashData(SHA256.HashData(hashInput));
        var unsigned = new byte[hash.Length + 1];
        hash.CopyTo(unsigned, 0);
        var hashVal = new BigInteger(unsigned);

        var target = new BigInteger(puzzle[14]) * BigInteger.Pow(2, 8 * (puzzle[13] - 3));
        return hashVal < target;
    }

    // Builds a Captcha-Puzzle header value: base64(single JWT string).
    // The solver doesn't verify JWT signatures, so we use a placeholder.
    private static string BuildCaptchaPuzzleHeader(string puzzleBase64) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(BuildJwt(puzzleBase64)));

    private static string BuildJwt(string puzzleBase64)
    {
        var payloadJson = $"{{\"puzzle\":\"{puzzleBase64}\",\"iat\":1000000000,\"exp\":1000000060}}";
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return $"eyJhbGciOiJIUzI1NiJ9.{payload}.placeholder";
    }

    private record SolvedItem(
        [property: JsonPropertyName("jwt")] string Jwt,
        [property: JsonPropertyName("solution")] string Solution);
}
