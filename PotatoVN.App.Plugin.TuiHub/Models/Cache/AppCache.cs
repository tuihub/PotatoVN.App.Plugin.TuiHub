using System;
using System.Collections.Generic;

namespace PotatoVN.App.Plugin.TuiHub.Models.Cache;

public class AppCache
{
    public DateTime LastSyncTime { get; set; } = DateTime.MinValue;
    public List<CachedApp> Apps { get; set; } = new();
    public List<CachedCategory> Categories { get; set; } = new();
}

