namespace PhotoScore.DotNet;

internal static class ImagePathEnumerator
{
    public static IReadOnlyList<string> Enumerate(string inputPath, bool recursive)
    {
        var fullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(inputPath));
        if (File.Exists(fullPath))
        {
            var extension = Path.GetExtension(fullPath);
            if (!SupportedImageExtensions.Values.Contains(extension))
            {
                throw new ArgumentException($"Unsupported image extension: {fullPath}");
            }

            return [fullPath];
        }

        if (!Directory.Exists(fullPath))
        {
            throw new FileNotFoundException($"Input path not found: {fullPath}", fullPath);
        }

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Directory
            .EnumerateFiles(fullPath, "*", searchOption)
            .Where(path => SupportedImageExtensions.Values.Contains(Path.GetExtension(path)))
            .Order(StringComparer.Ordinal)
            .ToArray();
    }
}
