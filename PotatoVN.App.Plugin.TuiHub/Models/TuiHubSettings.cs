using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PotatoVN.App.Plugin.TuiHub.Models;

public class TuiHubSettings : INotifyPropertyChanged
{
    private string _librarianUrl = "http://localhost:3100";
    private string _downloadRootDir = "";
    private string? _selectedLoginAccount;

    public string LibrarianUrl
    {
        get => _librarianUrl;
        set
        {
            if (_librarianUrl != value)
            {
                _librarianUrl = value;
                OnPropertyChanged();
            }
        }
    }

    public string DownloadRootDir
    {
        get => _downloadRootDir;
        set
        {
            if (_downloadRootDir != value)
            {
                _downloadRootDir = value;
                OnPropertyChanged();
            }
        }
    }

    public string? SelectedLoginAccount
    {
        get => _selectedLoginAccount;
        set
        {
            if (_selectedLoginAccount != value)
            {
                _selectedLoginAccount = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

