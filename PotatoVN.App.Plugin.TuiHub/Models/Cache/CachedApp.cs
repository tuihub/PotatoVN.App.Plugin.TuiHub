using System.Collections.Generic;

namespace PotatoVN.App.Plugin.TuiHub.Models.Cache;

public class CachedApp
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> CategoryIds { get; set; } = new();
    public string CoverImageUrl { get; set; } = string.Empty;
    public string IconImageUrl { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string Developer { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
}

