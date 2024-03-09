
namespace SMTPNET;
public static class MemoryExtensions
{
    public static bool StartsAs(this ReadOnlySpan<char> basicValue, ReadOnlySpan<char> comparingValue)
    {
        return basicValue.StartsWith(comparingValue, StringComparison.InvariantCultureIgnoreCase);
    }

    public static ReadOnlySpan<char> GetFirstTag(this ReadOnlySpan<char> chars)
    {
        return chars[(chars.IndexOf('<') + 1)..chars.IndexOf('>')];
    }
    public static string GetFirstTag(this string chars)
    {
        return chars[(chars.IndexOf('<') + 1)..chars.IndexOf('>')];
    }
}



