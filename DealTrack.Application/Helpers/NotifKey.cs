using System.Text.Json;

namespace DealTrack.Application.Helpers
{
    /// <summary>
    /// Builds localizable notification strings stored as {"k":"key","p":["p0"],"l":"/link"}.
    /// At read time NotificationService.LocalizeNotif() translates the key+params.
    /// </summary>
    public static class NotifKey
    {
        public static string Build(string key, params string[] args)
            => Build(key, args, null);

        public static string Build(string key, string[]? args, string? link)
        {
            var hasParams = args is { Length: > 0 };
            var hasLink   = !string.IsNullOrEmpty(link);

            if (!hasParams && !hasLink) return JsonSerializer.Serialize(new { k = key });
            if (!hasParams)             return JsonSerializer.Serialize(new { k = key, l = link });
            if (!hasLink)               return JsonSerializer.Serialize(new { k = key, p = args });
                                        return JsonSerializer.Serialize(new { k = key, p = args, l = link });
        }
    }
}
