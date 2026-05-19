namespace PhotoScore.DotNet;

internal static class SupportedImageExtensions
{
    public static readonly HashSet<string> Values = new(StringComparer.OrdinalIgnoreCase)
    {
        ".bmp",
        ".gif",
        ".jpeg",
        ".jpg",
        ".png",
        ".tif",
        ".tiff",
        ".webp",
    };
}
