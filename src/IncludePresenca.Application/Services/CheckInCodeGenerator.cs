using System.Globalization;
using System.Security.Cryptography;

namespace IncludePresenca.Application.Services;

public class CheckInCodeGenerator
{

    public string Generate()
    {
        return RandomNumberGenerator.GetInt32(0, 1_000_000)
            .ToString("D6", CultureInfo.InvariantCulture);
    }
}
