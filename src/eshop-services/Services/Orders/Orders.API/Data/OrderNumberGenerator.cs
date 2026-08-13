using System.Security.Cryptography;

namespace Orders.API.Data;

public static class OrderNumberGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string Generate(DateTime createdAtUtc)
    {
        return $"ORD-{createdAtUtc:yyyyMMdd}-{GenerateSuffix()}";
    }

    private static string GenerateSuffix()
    {
        Span<char> suffix = stackalloc char[6];

        for (var index = 0; index < suffix.Length; index++)
        {
            suffix[index] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new string(suffix);
    }
}
