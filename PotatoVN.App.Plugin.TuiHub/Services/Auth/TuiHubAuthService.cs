using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using GalgameManager.WinApp.Base.Contracts;
using Microsoft.UI.Xaml.Controls;
using PotatoVN.App.Plugin.TuiHub.Models;
using global::TuiHub.Protos.Librarian.Sephirah.V1;
using global::TuiHub.Protos.Librarian.Sentinel.V1;

namespace PotatoVN.App.Plugin.TuiHub.Services.Auth;

public class TuiHubAuthService
{
    private readonly IPotatoVnApi _api;
    private PluginData _data;
    private readonly object _lockObject = new();

    public TuiHubAuthService(IPotatoVnApi api, PluginData data)
    {
        _api = api;
        _data = data;
    }

    public async Task<bool> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        try
        {
            var channel = GrpcChannel.ForAddress(_data.Settings.LibrarianUrl);
            var client = new LibrarianSephirahService.LibrarianSephirahServiceClient(channel);

            var request = new GetTokenRequest
            {
                Username = username,
                Password = password
            };

            var response = await client.GetTokenAsync(request, cancellationToken: ct);

            lock (_lockObject)
            {
                _data.Tokens.AccessToken = response.AccessToken;
                _data.Tokens.RefreshToken = response.RefreshToken;
                // Access token 通常有效期为 1 小时
                _data.Tokens.AccessTokenExpiresAt = DateTime.UtcNow.AddHours(1);
                _data.Settings.SelectedLoginAccount = username;
            }

            await SaveDataAsync();
            
            _api.Info(InfoBarSeverity.Success, "登录成功", $"欢迎回来，{username}！");
            
            await channel.ShutdownAsync();
            return true;
        }
        catch (RpcException ex)
        {
            _api.Event(InfoBarSeverity.Error, "登录失败", ex, $"错误: {ex.Status.Detail}");
            return false;
        }
        catch (Exception ex)
        {
            _api.Event(InfoBarSeverity.Error, "登录失败", ex, "无法连接到 TuiHub 服务器");
            return false;
        }
    }

    public async Task RefreshTokenAsync(CancellationToken ct = default)
    {
        string refreshToken;
        lock (_lockObject)
        {
            refreshToken = _data.Tokens.RefreshToken;
        }

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new InvalidOperationException("No refresh token available");
        }

        try
        {
            var channel = GrpcChannel.ForAddress(_data.Settings.LibrarianUrl);
            var client = new global::TuiHub.Protos.Librarian.Sentinel.V1.LibrarianSephirahSentinelService.LibrarianSephirahSentinelServiceClient(channel);

            var headers = new Metadata
            {
                { "Authorization", $"Bearer {refreshToken}" }
            };

            var response = await client.RefreshTokenAsync(new global::TuiHub.Protos.Librarian.Sentinel.V1.RefreshTokenRequest(), headers, cancellationToken: ct);

            lock (_lockObject)
            {
                _data.Tokens.AccessToken = response.AccessToken;
                _data.Tokens.RefreshToken = response.RefreshToken;
                _data.Tokens.AccessTokenExpiresAt = DateTime.UtcNow.AddHours(1);
            }

            await SaveDataAsync();
            await channel.ShutdownAsync();
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unauthenticated)
        {
            // Refresh token 失效，清除所有 token
            ClearTokens();
            await SaveDataAsync();
            _api.Event(InfoBarSeverity.Warning, "登录已过期", null, "请重新登录");
            throw;
        }
    }

    public void Logout()
    {
        ClearTokens();
        _ = SaveDataAsync();
        _api.Info(InfoBarSeverity.Informational, "已退出登录");
    }

    public void ClearTokens()
    {
        lock (_lockObject)
        {
            _data.Tokens.AccessToken = string.Empty;
            _data.Tokens.RefreshToken = string.Empty;
            _data.Tokens.AccessTokenExpiresAt = DateTime.MinValue;
            _data.Settings.SelectedLoginAccount = null;
        }
    }

    public bool IsAccessTokenValid()
    {
        lock (_lockObject)
        {
            return _data.Tokens.IsAccessTokenValid();
        }
    }

    public bool HasRefreshToken()
    {
        lock (_lockObject)
        {
            return _data.Tokens.HasRefreshToken();
        }
    }

    public string GetAccessToken()
    {
        lock (_lockObject)
        {
            return _data.Tokens.AccessToken;
        }
    }

    public bool IsLoggedIn()
    {
        return IsAccessTokenValid() || HasRefreshToken();
    }

    private async Task SaveDataAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(_data);
            await _api.SaveDataAsync(json);
        }
        catch (Exception ex)
        {
            _api.Event(InfoBarSeverity.Error, "保存数据失败", ex);
        }
    }
}

