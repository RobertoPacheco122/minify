namespace Minify.Infrastructure.Services.Analytics;

public static class TrafficSourceDetector
{
    private static readonly Dictionary<string, string> KnownDomains =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["facebook.com"] = "facebook",
            ["l.facebook.com"] = "facebook",
            ["m.facebook.com"] = "facebook",
            ["wa.me"] = "whatsapp",
            ["web.whatsapp.com"] = "whatsapp",
            ["t.co"] = "twitter",
            ["twitter.com"] = "twitter",
            ["x.com"] = "twitter",
            ["instagram.com"] = "instagram",
            ["l.instagram.com"] = "instagram",
            ["mail.google.com"] = "email",
            ["outlook.live.com"] = "email",
            ["outlook.com"] = "email",
            ["yahoo.com"] = "email"
        };

    public static string Detect(string? utmSource, string? referer)
    {
        if (!string.IsNullOrWhiteSpace(utmSource))
            return utmSource.ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(referer))
            return "direct";

        if (!Uri.TryCreate(referer, UriKind.Absolute, out var uri))
            return "direct";

        return KnownDomains.TryGetValue(uri.Host, out var source) ? source : "organic";
    }
}
