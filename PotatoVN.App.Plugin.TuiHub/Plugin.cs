using GalgameManager.WinApp.Base.Contracts;
using GalgameManager.WinApp.Base.Contracts.PluginUi;
using GalgameManager.WinApp.Base.Models;
using PotatoVN.App.Plugin.TuiHub.BgTasks;
using PotatoVN.App.Plugin.TuiHub.Models;
using PotatoVN.App.Plugin.TuiHub.Services.Auth;
using PotatoVN.App.Plugin.TuiHub.Services.Cache;
using PotatoVN.App.Plugin.TuiHub.Services.Grpc;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace PotatoVN.App.Plugin.TuiHub;

public partial class Plugin : IPlugin, IPluginSetting
{
    private IPotatoVnApi _hostApi = null!;
    private PluginData _data = new();
    private TuiHubAuthService? _authService;
    private TuiHubCacheStore? _cacheStore;
    private ClientTokenInterceptor? _tokenInterceptor;
    private GrpcChannelFactory? _grpcFactory;

    public PluginInfo Info { get; } = new()
    {
        Id = new Guid("aab968fd-bc0f-4edf-b86c-e7ddbfd49ff1"),
        Name = "TuiHub",
        Description = "连接到 TuiHub 服务器，同步和下载应用"
    };

    public async Task InitializeAsync(IPotatoVnApi hostApi)
    {
        _hostApi = hostApi;
        // 为 XAML 资源定位器设置插件根路径，确保 LoadComponent 能正确找到插件内的 XAML
        XamlResourceLocatorFactory.packagePath = _hostApi.GetPluginPath();

        // 加载数据
        var dataJson = await _hostApi.GetDataAsync();
        if (!string.IsNullOrWhiteSpace(dataJson))
        {
            try
            {
                _data = JsonSerializer.Deserialize<PluginData>(dataJson) ?? new PluginData();
            }
            catch
            {
                _data = new PluginData();
            }
        }

        // 初始化服务
        _authService = new TuiHubAuthService(_hostApi, _data);
        _cacheStore = new TuiHubCacheStore(_hostApi);
        _tokenInterceptor = new ClientTokenInterceptor(_authService);
        _grpcFactory = new GrpcChannelFactory(_data.Settings.LibrarianUrl, _tokenInterceptor);

        // 监听设置变化以更新 gRPC 工厂
        _data.Settings.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(TuiHubSettings.LibrarianUrl))
            {
                _grpcFactory?.Dispose();
                _grpcFactory = new GrpcChannelFactory(_data.Settings.LibrarianUrl, _tokenInterceptor);
            }
            SaveData();
        };

        _data.PropertyChanged += (s, e) => SaveData();

        // 添加测试用的后台任务
        _ = _hostApi.AddBgTask(new DummyBgTask1());
        _ = _hostApi.AddBgTask(new DummyBgTask2());
    }

    private void SaveData()
    {
        try
        {
            var dataJson = JsonSerializer.Serialize(_data);
            _ = _hostApi.SaveDataAsync(dataJson);
        }
        catch (Exception ex)
        {
            _hostApi.DeveloperEvent(Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error, "保存插件数据失败", ex);
        }
    }
}

