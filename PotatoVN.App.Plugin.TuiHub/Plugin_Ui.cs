using System;
using Microsoft.UI.Xaml;
using PotatoVN.App.Plugin.TuiHub.Controls.Prefabs;
using PotatoVN.App.Plugin.TuiHub.Helper;
using PotatoVN.App.Plugin.TuiHub.UI.Settings;

namespace PotatoVN.App.Plugin.TuiHub;

public partial class Plugin
{
    public FrameworkElement CreateSettingUi()
    {
        if (_authService == null || _cacheStore == null || _grpcFactory == null)
        {
            throw new InvalidOperationException("Plugin not initialized");
        }

        StdStackPanel panel = new();
        panel.Children.Add(new TuiHubSettingsPage(_hostApi, _data, _authService, _cacheStore, _grpcFactory).WarpWithPanel());
        return panel;
    }
}