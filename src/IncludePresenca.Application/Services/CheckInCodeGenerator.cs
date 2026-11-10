using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace IncludePresenca.Application.Services;

public class CheckInCodeGenerator
{

    public string Generate()
    {
        return RandomNumberGenerator.GetInt32(0, 1_000_000)
            .ToString("D6", CultureInfo.InvariantCulture);
    }

    public string Hash(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(code)));
    }

    public bool Matches(string code, string hash)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(hash))
            return false;

        byte[] expected;

        try
        {
            expected = Convert.FromHexString(hash);
        }
        catch (FormatException)
        {
            return false;
        }

        byte[] actual = SHA256.HashData(Encoding.UTF8.GetBytes(code));

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
