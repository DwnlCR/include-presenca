namespace IncludePresenca.Application.Services;

public class CheckInCodeValidator
{

    public bool IsValid(string? code)
    {
        if (code is null || code.Length != 6)
        {
            return false;
        }

        foreach (char digit in code)
        {
            if (digit < '0' || digit > '9')
            {
                return false;
            }
        }

        return true;
    }
}
