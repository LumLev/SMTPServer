
using System.Runtime.CompilerServices;
using System.Text;

namespace SMTPNET;
public static class MemoryExtensions
{
    public static bool StartsAs(this ReadOnlySpan<char> basicValue, ReadOnlySpan<char> comparingValue)
    {
        return basicValue.StartsWith(comparingValue,
            StringComparison.InvariantCultureIgnoreCase);
    }

    public static bool EndsAs(this ReadOnlySpan<char> basicValue, ReadOnlySpan<char> comparingValue)
    {
        return basicValue.EndsWith(comparingValue,
            StringComparison.InvariantCultureIgnoreCase);
    }

    public static ReadOnlySpan<char> GetFirstTag(this ReadOnlySpan<char> chars)
    {
        return chars[(chars.IndexOf('<') + 1)..chars.IndexOf('>')].Trim();
    }

    public static ReadOnlySpan<byte> ToUTF8(this string chars) 
    {
        return Encoding.UTF8.GetBytes(chars);
    }

    public static bool EndsAs(this byte[] bytes, string endswithchars)
    {
        return Encoding.UTF8.GetString(bytes).Equals(endswithchars, 
            StringComparison.InvariantCultureIgnoreCase);
    }

    public static bool EndsAs(this Span<byte> bytes, string endswithchars)
    {
        return Encoding.UTF8.GetString(bytes).Equals(endswithchars,
            StringComparison.InvariantCultureIgnoreCase);
    }

    public static string GetFirstTag(this string chars)
    {
        return chars[(chars.IndexOf('<') + 1)..chars.IndexOf('>')];
    }
}



