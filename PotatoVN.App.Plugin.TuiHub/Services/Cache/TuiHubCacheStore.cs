using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using GalgameManager.WinApp.Base.Contracts;
using Microsoft.UI.Xaml.Controls;
using PotatoVN.App.Plugin.TuiHub.Models.Cache;

namespace PotatoVN.App.Plugin.TuiHub.Services.Cache;

public class TuiHubCacheStore
{
    private readonly IPotatoVnApi _api;
    private readonly string _cacheFilePath;
    private AppCache? _cache;
    private readonly object _lockObject = new();

    public TuiHubCacheStore(IPotatoVnApi api)
    {
        _api = api;
        var pluginPath = _api.GetPluginPath();
        _cacheFilePath = Path.Combine(pluginPath, "app_cache.json");
    }

    public async Task<AppCache> LoadCacheAsync()
    {
        lock (_lockObject)
        {
            if (_cache != null)
            {
                return _cache;
            }
        }

        try
        {
            if (File.Exists(_cacheFilePath))
            {
                var json = await File.ReadAllTextAsync(_cacheFilePath);
                var cache = JsonSerializer.Deserialize<AppCache>(json);
                
                lock (_lockObject)
                {
                    _cache = cache ?? new AppCache();
                    return _cache;
                }
            }
        }
        catch (Exception ex)
        {
            _api.DeveloperEvent(InfoBarSeverity.Warning, "加载缓存失败", ex);
        }

        lock (_lockObject)
        {
            _cache = new AppCache();
            return _cache;
        }
    }

    public async Task SaveCacheAsync(AppCache cache)
    {
        try
        {
            lock (_lockObject)
            {
                _cache = cache;
            }

            var json = JsonSerializer.Serialize(cache, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var directory = Path.GetDirectoryName(_cacheFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(_cacheFilePath, json);
        }
        catch (Exception ex)
        {
            _api.Event(InfoBarSeverity.Error, "保存缓存失败", ex);
        }
    }

    public AppCache? GetCache()
    {
        lock (_lockObject)
        {
            return _cache;
        }
    }

    public void ClearCache()
    {
        lock (_lockObject)
        {
            _cache = new AppCache();
        }

        try
        {
            if (File.Exists(_cacheFilePath))
            {
                File.Delete(_cacheFilePath);
            }
        }
        catch (Exception ex)
        {
            _api.DeveloperEvent(InfoBarSeverity.Warning, "清除缓存失败", ex);
        }
    }
}

