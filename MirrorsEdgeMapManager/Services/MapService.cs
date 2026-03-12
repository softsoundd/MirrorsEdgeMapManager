using MirrorsEdgeMapManager.Models;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;

namespace MirrorsEdgeMapManager.Services;

public class MapService
{
    private const string MapsJsonUrl = "https://github.com/softsoundd/MirrorsEdgeMapManager/raw/main/Maps.json";
    private readonly HttpClient _httpClient;
    private readonly PathService _pathService;

    public MapService(PathService pathService)
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _pathService = pathService;
    }

    public async Task<Dictionary<string, List<MapEntry>>> FetchMapsAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync(MapsJsonUrl);
            var maps = JsonSerializer.Deserialize<Dictionary<string, List<MapEntry>>>(response);
            return maps ?? new Dictionary<string, List<MapEntry>>();
        }
        catch
        {
            return new Dictionary<string, List<MapEntry>>();
        }
    }

    public bool IsMapInstalled(string mapName, string memmFolderPath, string memmInactiveFolderPath)
    {
        var activePath = Path.Combine(memmFolderPath, mapName);
        var inactivePath = Path.Combine(memmInactiveFolderPath, mapName);

        if (Directory.Exists(activePath) || Directory.Exists(inactivePath))
            return true;

        if (Directory.Exists(memmFolderPath))
        {
            foreach (var dir in Directory.GetDirectories(memmFolderPath, "*", SearchOption.AllDirectories))
            {
                if (Path.GetFileName(dir) == mapName)
                    return true;
            }
        }

        if (Directory.Exists(memmInactiveFolderPath))
        {
            foreach (var dir in Directory.GetDirectories(memmInactiveFolderPath, "*", SearchOption.AllDirectories))
            {
                if (Path.GetFileName(dir) == mapName)
                    return true;
            }
        }

        return false;
    }

    public async Task<(bool success, string message)> DownloadMapAsync(
        string url, 
        string friendlyName, 
        string targetPath,
        IProgress<(int percentage, string status)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var totalSize = response.Content.Headers.ContentLength ?? 0;
            var zipPath = Path.Combine(targetPath, "map.zip");

            using (var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken))
            using (var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
            {
                var buffer = new byte[8192];
                var totalBytesRead = 0L;
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                    totalBytesRead += bytesRead;

                    if (totalSize > 0)
                    {
                        var percentage = (int)((totalBytesRead * 100) / totalSize);
                        var sizeMb = totalSize / (1024.0 * 1024.0);
                        progress?.Report((percentage, $"Downloading {friendlyName}... ({sizeMb:F2} MB)"));
                    }
                }
            }

            progress?.Report((100, $"Extracting {friendlyName}..."));
            ZipFile.ExtractToDirectory(zipPath, targetPath, true);
            File.Delete(zipPath);

            return (true, $"Successfully downloaded {friendlyName}");
        }
        catch (OperationCanceledException)
        {
            return (false, "Download cancelled");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to download {friendlyName}: {ex.Message}");
        }
    }

    public async Task<string> FetchConfigContentAsync(string configUrl)
    {
        try
        {
            return await _httpClient.GetStringAsync(configUrl);
        }
        catch
        {
            return string.Empty;
        }
    }

    public (bool success, string message) UninstallMap(string mapName, string memmFolderPath)
    {
        try
        {
            var mapPath = Path.Combine(memmFolderPath, mapName);
            
            if (!Directory.Exists(mapPath))
            {
                foreach (var dir in Directory.GetDirectories(memmFolderPath, "*", SearchOption.AllDirectories))
                {
                    if (Path.GetFileName(dir) == mapName)
                    {
                        mapPath = dir;
                        break;
                    }
                }
            }

            if (Directory.Exists(mapPath))
            {
                Directory.Delete(mapPath, true);
                return (true, $"Successfully uninstalled {mapName}");
            }

            return (false, $"Map folder not found: {mapName}");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to uninstall {mapName}: {ex.Message}");
        }
    }

    public async Task<byte[]?> DownloadImageAsync(string imageUrl)
    {
        try
        {
            return await _httpClient.GetByteArrayAsync(imageUrl);
        }
        catch
        {
            return null;
        }
    }
}

