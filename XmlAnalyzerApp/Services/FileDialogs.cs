using CommunityToolkit.Maui.Storage;

namespace XmlAnalyzerApp.Services;

public static class FileDialogs
{
    private static readonly FilePickerFileType XmlType = new(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        [DevicePlatform.MacCatalyst] = ["public.xml"],
        [DevicePlatform.iOS] = ["public.xml"],
        [DevicePlatform.WinUI] = [".xml"],
        [DevicePlatform.Android] = ["text/xml", "application/xml"],
    });

    private static readonly FilePickerFileType XslType = new(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        [DevicePlatform.MacCatalyst] = ["public.xml", "public.data"],
        [DevicePlatform.iOS] = ["public.xml", "public.data"],
        [DevicePlatform.WinUI] = [".xsl", ".xslt"],
        [DevicePlatform.Android] = ["*/*"],
    });

    public static async Task<FileResult?> PickXmlAsync() =>
        await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Відкрити XML-файл", FileTypes = XmlType });

    public static async Task<string?> PickXslAsync()
    {
        var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Обрати XSL-файл", FileTypes = XslType });
        if (file is null) return null;
        string ext = Path.GetExtension(file.FileName);
        if (!ext.Equals(".xsl", StringComparison.OrdinalIgnoreCase) && !ext.Equals(".xslt", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Оберіть файл із розширенням .xsl або .xslt.");
        return file.FullPath;
    }

    public static async Task<string> ExtractAssetAsync(string name)
    {
        string target = Path.Combine(FileSystem.CacheDirectory, name);
        await using var source = await FileSystem.OpenAppPackageFileAsync(name);
        await using var output = File.Create(target);
        await source.CopyToAsync(output);
        return target;
    }

    public static string NewTempPath(string extension) =>
        Path.Combine(FileSystem.CacheDirectory, $"export-{Guid.NewGuid():N}{extension}");

    public static async Task<string?> SaveCopyAsync(string sourcePath, string suggestedName, bool keepSource = false)
    {
        try
        {
            await using var stream = File.OpenRead(sourcePath);
            var result = await FileSaver.Default.SaveAsync(suggestedName, stream, CancellationToken.None);
            if (result.IsSuccessful) return result.FilePath;
            if (result.Exception is null or OperationCanceledException) return null;
            if (result.Exception.Message.Contains("cancel", StringComparison.OrdinalIgnoreCase)) return null;
            throw new IOException($"Не вдалося зберегти файл: {result.Exception.Message}", result.Exception);
        }
        finally
        {
            if (!keepSource) File.Delete(sourcePath);
        }
    }
}
