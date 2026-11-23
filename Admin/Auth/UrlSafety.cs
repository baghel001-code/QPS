namespace Admin.Auth;

public static class UrlSafety
{
    public const string RejectedPath = "/nopage";

    public static string LocalOrRoot(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "/";
        if (!IsLocal(url))
            return RejectedPath;
        // Returning to the login/logout pages after signing in would be a loop. Admin/Components/Pages/NoPage.razor
        if (url.StartsWith("/Account/Log", StringComparison.OrdinalIgnoreCase))
            return "/";
        return url;
    }

    public static bool IsLocal(string url) =>
        url[0] == '/' && !(url.Length > 1 && (url[1] == '/' || url[1] == '\\')) && !url.Any(char.IsControl); // "//host" and "/\host" are treated as absolute URLs by browsers.
}