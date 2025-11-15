using System;

namespace PotatoVN.App.Plugin.TuiHub.Models;

public class Tokens
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; } = DateTime.MinValue;

    public bool IsAccessTokenValid()
    {
        return !string.IsNullOrWhiteSpace(AccessToken) && 
               DateTime.UtcNow < AccessTokenExpiresAt.AddMinutes(-5); // 5分钟缓冲
    }

    public bool HasRefreshToken()
    {
        return !string.IsNullOrWhiteSpace(RefreshToken);
    }
}

