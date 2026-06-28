namespace EAPlaymateGroup.Services;

public static class ClientInfoParser
{
    public static string? ToDeviceInfo(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return null;
        }

        var ua = userAgent.Trim();
        var device = ua.Contains("Mobile", StringComparison.OrdinalIgnoreCase) ||
                     ua.Contains("Android", StringComparison.OrdinalIgnoreCase) ||
                     ua.Contains("iPhone", StringComparison.OrdinalIgnoreCase)
            ? "Mobile"
            : "Desktop";
        var os = ua.Contains("Windows", StringComparison.OrdinalIgnoreCase) ? "Windows"
            : ua.Contains("Mac OS", StringComparison.OrdinalIgnoreCase) ? "macOS"
            : ua.Contains("Android", StringComparison.OrdinalIgnoreCase) ? "Android"
            : ua.Contains("iPhone", StringComparison.OrdinalIgnoreCase) || ua.Contains("iPad", StringComparison.OrdinalIgnoreCase) ? "iOS"
            : ua.Contains("Linux", StringComparison.OrdinalIgnoreCase) ? "Linux"
            : "Unknown OS";
        var browser = ua.Contains("Edg/", StringComparison.OrdinalIgnoreCase) ? "Edge"
            : ua.Contains("Chrome/", StringComparison.OrdinalIgnoreCase) ? "Chrome"
            : ua.Contains("Firefox/", StringComparison.OrdinalIgnoreCase) ? "Firefox"
            : ua.Contains("Safari/", StringComparison.OrdinalIgnoreCase) ? "Safari"
            : "Unknown Browser";

        return $"{device} / {os} / {browser}";
    }
}
