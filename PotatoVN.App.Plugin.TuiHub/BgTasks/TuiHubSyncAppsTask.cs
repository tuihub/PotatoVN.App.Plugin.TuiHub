using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GalgameManager.Models.BgTasks;
using GalgameManager.WinApp.Base.Contracts;
using Microsoft.UI.Xaml.Controls;
using PotatoVN.App.Plugin.TuiHub.Models.Cache;
using PotatoVN.App.Plugin.TuiHub.Services.Auth;
using PotatoVN.App.Plugin.TuiHub.Services.Cache;
using PotatoVN.App.Plugin.TuiHub.Services.Grpc;
using global::TuiHub.Protos.Librarian.Sephirah.V1;

namespace PotatoVN.App.Plugin.TuiHub.BgTasks;

public class TuiHubSyncAppsTask : BgTaskBase
{
    private readonly IPotatoVnApi _api;
    private readonly TuiHubAuthService _authService;
    private readonly GrpcChannelFactory _grpcFactory;
    private readonly TuiHubCacheStore _cacheStore;

    public override string Title => "同步 TuiHub 应用库";
    public override bool CanCancel => true;

    public TuiHubSyncAppsTask(
        IPotatoVnApi api,
        TuiHubAuthService authService,
        GrpcChannelFactory grpcFactory,
        TuiHubCacheStore cacheStore)
    {
        _api = api;
        _authService = authService;
        _grpcFactory = grpcFactory;
        _cacheStore = cacheStore;
    }

    protected override async Task RunInternal()
    {
        CancellationTokenSource = new CancellationTokenSource();
        var ct = CancellationToken!.Value;

        try
        {
            if (!_authService.IsLoggedIn())
            {
                ChangeProgress(-1, 1, "未登录，请先登录 TuiHub");
                return;
            }

            ChangeProgress(0, 100, "开始同步应用和分类...", false);

            var client = new LibrarianSephirahService.LibrarianSephirahServiceClient(_grpcFactory.GetCallInvoker());

            // 1. 获取分类列表
            ChangeProgress(10, 100, "获取分类列表...", false);
            var categoriesResponse = await client.ListAppCategoriesAsync(
                new ListAppCategoriesRequest(), 
                cancellationToken: ct);

            var cachedCategories = categoriesResponse.AppCategories
                .Select(c => new CachedCategory
                {
                    Id = c.Id.Id.ToString(),
                    Name = c.Name,
                    AppIds = c.AppIds.Select(id => id.Id.ToString()).ToList()
                })
                .ToList();

            ct.ThrowIfCancellationRequested();

            // 2. 获取应用列表（分页）
            ChangeProgress(30, 100, "获取应用列表...", false);
            var apps = new List<CachedApp>();
            var pageSize = 50;
            var pageNum = 1;
            var hasMore = true;

            while (hasMore && !ct.IsCancellationRequested)
            {
                var appsRequest = new ListAppsRequest
                {
                    Paging = new global::TuiHub.Protos.Librarian.V1.PagingRequest
                    {
                        PageSize = pageSize,
                        PageNum = pageNum
                    }
                };

                var appsResponse = await client.ListAppsAsync(appsRequest, cancellationToken: ct);

                foreach (var app in appsResponse.Apps)
                {
                    // 找出该应用所属的分类
                    var categoryIds = cachedCategories
                        .Where(c => c.AppIds.Contains(app.Id.Id.ToString()))
                        .Select(c => c.Id)
                        .ToList();

                    apps.Add(new CachedApp
                    {
                        Id = app.Id.Id.ToString(),
                        Name = app.Name,
                        Description = app.Description,
                        CategoryIds = categoryIds,
                        CoverImageUrl = app.CoverImageUrl,
                        IconImageUrl = app.IconImageUrl,
                        Tags = app.Tags.ToList(),
                        Developer = app.Developer,
                        Publisher = app.Publisher
                    });
                }

                hasMore = appsResponse.Paging.TotalSize > pageNum * pageSize;
                pageNum++;

                var progress = 30 + (int)((pageNum - 1) * 50.0 / Math.Max(1, appsResponse.Paging.TotalSize / pageSize));
                ChangeProgress(progress, 100, $"已获取 {apps.Count} 个应用...", false);
            }

            ct.ThrowIfCancellationRequested();

            // 3. 保存缓存
            ChangeProgress(90, 100, "保存缓存...", false);
            var cache = new AppCache
            {
                LastSyncTime = DateTime.UtcNow,
                Apps = apps,
                Categories = cachedCategories
            };

            await _cacheStore.SaveCacheAsync(cache);

            ChangeProgress(100, 100, $"同步完成！共 {apps.Count} 个应用，{cachedCategories.Count} 个分类");
            _api.Info(InfoBarSeverity.Success, "同步完成", $"已同步 {apps.Count} 个应用");
        }
        catch (OperationCanceledException)
        {
            ChangeProgress(-1, 1, "同步已取消");
        }
        catch (Exception ex)
        {
            ChangeProgress(-1, 1, $"同步失败: {ex.Message}");
            _api.Event(InfoBarSeverity.Error, "同步失败", ex);
        }
        finally
        {
            CancellationTokenSource?.Dispose();
            CancellationTokenSource = null;
        }
    }

    protected override Task RecoverFromJsonInternal()
    {
        // 从 JSON 恢复时不执行任何操作，标记为失败
        ChangeProgress(-1, 1, "任务已过期，请重新同步");
        return Task.CompletedTask;
    }
}

