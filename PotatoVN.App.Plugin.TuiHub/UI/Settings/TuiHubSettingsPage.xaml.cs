using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using GalgameManager.WinApp.Base.Contracts;
using Microsoft.UI.Xaml.Controls.Primitives;
using PotatoVN.App.Plugin.TuiHub.Models;
using PotatoVN.App.Plugin.TuiHub.Services.Auth;
using PotatoVN.App.Plugin.TuiHub.Services.Cache;
using PotatoVN.App.Plugin.TuiHub.BgTasks;
using PotatoVN.App.Plugin.TuiHub.Services.Grpc;

namespace PotatoVN.App.Plugin.TuiHub.UI.Settings;

public sealed partial class TuiHubSettingsPage : UserControl
{
    private readonly IPotatoVnApi _api;
    private readonly PluginData _data;
    private readonly TuiHubAuthService _authService;
    private readonly TuiHubCacheStore _cacheStore;
    private readonly GrpcChannelFactory _grpcFactory;

    public TuiHubSettingsPage(
        IPotatoVnApi api,
        PluginData data,
        TuiHubAuthService authService,
        TuiHubCacheStore cacheStore,
        GrpcChannelFactory grpcFactory)
    {
        PluginInitializeComponent();
        _api = api;
        _data = data;
        _authService = authService;
        _cacheStore = cacheStore;
        _grpcFactory = grpcFactory;

        LoadSettings();
        UpdateLoginStatus();
        UpdateCacheInfo();
    }
    
    private void PluginInitializeComponent()
    {
        if (_contentLoaded)
            return;

        _contentLoaded = true;

        var resourceLocator = XamlResourceLocatorFactory.Create();
        Application.LoadComponent(this, resourceLocator, ComponentResourceLocation.Application);
    }

    private void LoadSettings()
    {
        LibrarianUrlTextBox.Text = _data.Settings.LibrarianUrl;
        DownloadRootDirTextBox.Text = _data.Settings.DownloadRootDir;

        LibrarianUrlTextBox.TextChanged += (s, e) =>
        {
            _data.Settings.LibrarianUrl = LibrarianUrlTextBox.Text;
        };

        DownloadRootDirTextBox.TextChanged += (s, e) =>
        {
            _data.Settings.DownloadRootDir = DownloadRootDirTextBox.Text;
        };
    }

    private void UpdateLoginStatus()
    {
        var isLoggedIn = _authService.IsLoggedIn();
        LoginPanel.Visibility = isLoggedIn ? Visibility.Collapsed : Visibility.Visible;
        LoggedInPanel.Visibility = isLoggedIn ? Visibility.Visible : Visibility.Collapsed;

        if (isLoggedIn && !string.IsNullOrWhiteSpace(_data.Settings.SelectedLoginAccount))
        {
            AccountInfoTextBlock.Text = $"已登录: {_data.Settings.SelectedLoginAccount}";
        }
    }

    private async void UpdateCacheInfo()
    {
        var cache = await _cacheStore.LoadCacheAsync();
        if (cache.LastSyncTime == DateTime.MinValue)
        {
            CacheInfoTextBlock.Text = "尚未同步";
        }
        else
        {
            CacheInfoTextBlock.Text = $"最后同步: {cache.LastSyncTime.ToLocalTime():yyyy-MM-dd HH:mm:ss}\n" +
                                     $"应用数: {cache.Apps.Count}, 分类数: {cache.Categories.Count}";
        }
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        var username = UsernameTextBox.Text;
        var password = PasswordBox.Password;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _api.Info(InfoBarSeverity.Warning, "请输入用户名和密码");
            return;
        }

        LoginButton.IsEnabled = false;
        try
        {
            var success = await _authService.LoginAsync(username, password);
            if (success)
            {
                PasswordBox.Password = string.Empty;
                UpdateLoginStatus();
            }
        }
        finally
        {
            LoginButton.IsEnabled = true;
        }
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        _authService.Logout();
        UpdateLoginStatus();
    }

    private async void SyncButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_data.Settings.LibrarianUrl))
        {
            _api.Info(InfoBarSeverity.Warning, "请先设置服务器地址");
            return;
        }

        SyncButton.IsEnabled = false;
        try
        {
            var task = new TuiHubSyncAppsTask(_api, _authService, _grpcFactory, _cacheStore);
            await _api.AddBgTask(task);
            _api.Info(InfoBarSeverity.Informational, "同步任务已启动", "请在后台任务中查看进度");
            
            // 等待一会儿再更新缓存信息
            await Task.Delay(2000);
            UpdateCacheInfo();
        }
        finally
        {
            SyncButton.IsEnabled = true;
        }
    }

    private void ClearCacheButton_Click(object sender, RoutedEventArgs e)
    {
        _cacheStore.ClearCache();
        UpdateCacheInfo();
        _api.Info(InfoBarSeverity.Success, "缓存已清除");
    }
}

