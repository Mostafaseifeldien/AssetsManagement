namespace AssetsManagement.Application;

public static class YesNoParser
{
    public static bool? TryParse(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "yes" or "true" => true,
        "no" or "false" => false,
        _ => null
    };

    public static bool IsYesNo(string? value) => TryParse(value).HasValue;
    public static string Format(bool value) => value ? "Yes" : "No";
}
