using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PotatoVN.App.Plugin.TuiHub.Models;

public class PluginData : INotifyPropertyChanged
{
    public TuiHubSettings Settings { get; set; } = new();
    public Tokens Tokens { get; set; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

