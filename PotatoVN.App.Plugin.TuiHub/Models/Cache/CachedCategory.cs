using System.Collections.Generic;

namespace PotatoVN.App.Plugin.TuiHub.Models.Cache;

public class CachedCategory
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> AppIds { get; set; } = new();
}

