using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Downloader;
using GalgameManager.Models.BgTasks;
using GalgameManager.WinApp.Base.Contracts;
using Microsoft.UI.Xaml.Controls;
using PotatoVN.App.Plugin.TuiHub.Services.Auth;
using PotatoVN.App.Plugin.TuiHub.Services.Grpc;
using global::TuiHub.Protos.Librarian.Sephirah.V1;
using global::TuiHub.Protos.Librarian.V1;

namespace PotatoVN.App.Plugin.TuiHub.BgTasks;

public class TuiHubDownloadTask : BgTaskBase
{
    private readonly IPotatoVnApi _api;
    private readonly TuiHubAuthService _authService;
    private readonly GrpcChannelFactory _grpcFactory;
    private readonly string _appId;
    private readonly string _appName;
    private readonly string _binaryId;
    private readonly string _downloadRootDir;
    private DownloadService? _downloadService;

    public override string Title => $"下载 {_appName}";
    public override bool ProgressOnTrayIcon => true;
    public override bool CanCancel => true;

    public TuiHubDownloadTask(
        IPotatoVnApi api,
        TuiHubAuthService authService,
        GrpcChannelFactory grpcFactory,
        string appId,
        string appName,
        string binaryId,
        string downloadRootDir)
    {
        _api = api;
        _authService = authService;
        _grpcFactory = grpcFactory;
        _appId = appId;
        _appName = appName;
        _binaryId = binaryId;
        _downloadRootDir = downloadRootDir;
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

            ChangeProgress(0, 100, "准备下载...", false);

            var client = new LibrarianSephirahService.LibrarianSephirahServiceClient(_grpcFactory.GetCallInvoker());

            // 1. 获取下载信息
            var downloadRequest = new DownloadStoreAppBinaryRequest
            {
                Id = new InternalID { Id = long.Parse(_binaryId) }
            };

            var downloadResponse = await client.DownloadStoreAppBinaryAsync(downloadRequest, cancellationToken: ct);

            if (string.IsNullOrWhiteSpace(downloadResponse.DownloadBaseUrl))
            {
                ChangeProgress(-1, 1, "无法获取下载地址");
                return;
            }

            // 2. 获取文件列表
            var filesRequest = new ListStoreAppBinaryFilesRequest
            {
                AppBinaryId = new InternalID { Id = long.Parse(_binaryId) },
                Paging = new PagingRequest { PageSize = 1000, PageNum = 1 }
            };

            var filesResponse = await client.ListStoreAppBinaryFilesAsync(filesRequest, cancellationToken: ct);

            if (!filesResponse.BinaryFiles.Any())
            {
                ChangeProgress(-1, 1, "没有可下载的文件");
                return;
            }

            // 3. 准备下载目录
            var downloadingDir = Path.Combine(_downloadRootDir, "downloading", _appId);
            var tempDir = Path.Combine(_downloadRootDir, "temp", _appId);
            var commonDir = Path.Combine(_downloadRootDir, "common", _appName);

            Directory.CreateDirectory(downloadingDir);
            Directory.CreateDirectory(tempDir);

            ct.ThrowIfCancellationRequested();

            // 4. 下载文件
            var totalFiles = filesResponse.BinaryFiles.Count;
            var completedFiles = 0;

            foreach (var file in filesResponse.BinaryFiles)
            {
                if (ct.IsCancellationRequested) break;

                var fileUrl = $"{downloadResponse.DownloadBaseUrl.TrimEnd('/')}/{file.DownloadPath.TrimStart('/')}";
                var localPath = Path.Combine(downloadingDir, file.File.Name);

                // 确保子目录存在
                var fileDir = Path.GetDirectoryName(localPath);
                if (!string.IsNullOrEmpty(fileDir))
                {
                    Directory.CreateDirectory(fileDir);
                }

                ChangeProgress(
                    completedFiles * 100,
                    totalFiles * 100,
                    $"下载 {file.File.Name} ({completedFiles + 1}/{totalFiles})...",
                    false);

                // 使用 Downloader 库下载
                var downloadOpt = new DownloadConfiguration
                {
                    ChunkCount = 4,
                    ParallelDownload = true,
                    MaxTryAgainOnFailure = 3,
                    Timeout = 10000,
                };

                _downloadService = new DownloadService(downloadOpt);

                // 注册取消回调，确保 DownloadService 能够快速响应取消
                ct.Register(() => _downloadService?.CancelAsync());

                // 进度回调
                _downloadService.DownloadProgressChanged += (sender, e) =>
                {
                    var fileProgress = (int)(e.ProgressPercentage);
                    var totalProgress = completedFiles * 100 + fileProgress;
                    ChangeProgress(
                        totalProgress,
                        totalFiles * 100,
                        $"下载 {file.File.Name} ({e.ProgressPercentage:F1}%)...",
                        false);
                };

                await _downloadService.DownloadFileTaskAsync(fileUrl, localPath, ct);

                completedFiles++;
            }

            if (ct.IsCancellationRequested)
            {
                ChangeProgress(-1, 1, "下载已取消");
                return;
            }

            // 5. 移动到 common 目录
            ChangeProgress(95, 100, "整理文件...", false);
            Directory.CreateDirectory(commonDir);

            foreach (var file in Directory.GetFiles(downloadingDir, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(downloadingDir, file);
                var targetPath = Path.Combine(commonDir, relativePath);
                var targetDir = Path.GetDirectoryName(targetPath);
                
                if (!string.IsNullOrEmpty(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                File.Move(file, targetPath, true);
            }

            // 清理 downloading 目录
            if (Directory.Exists(downloadingDir))
            {
                Directory.Delete(downloadingDir, true);
            }

            // 6. PostDownload 占位
            await PostDownloadAsync(commonDir, ct);

            ChangeProgress(100, 100, $"下载完成！文件保存在: {commonDir}");
            _api.Info(InfoBarSeverity.Success, "下载完成", _appName);

            EventAction = () =>
            {
                try
                {
                    System.Diagnostics.Process.Start("explorer.exe", commonDir);
                }
                catch { }
            };
            EventActionText = "打开文件夹";
        }
        catch (OperationCanceledException)
        {
            ChangeProgress(-1, 1, "下载已取消");
        }
        catch (Exception ex)
        {
            ChangeProgress(-1, 1, $"下载失败: {ex.Message}");
            _api.Event(InfoBarSeverity.Error, "下载失败", ex);
        }
        finally
        {
            _downloadService?.Dispose();
            _downloadService = null;
            CancellationTokenSource?.Dispose();
            CancellationTokenSource = null;
        }
    }

    /// <summary>
    /// PostDownload 占位方法，用于后续实现解压/安装逻辑
    /// </summary>
    private async Task PostDownloadAsync(string targetDir, CancellationToken ct)
    {
        // TODO: 实现解压/安装逻辑
        // 例如：
        // - 检测压缩文件并解压
        // - 运行安装程序
        // - 创建快捷方式
        // - 注册到主程序游戏库
        await Task.Delay(100, ct); // 占位
    }

    protected override Task RecoverFromJsonInternal()
    {
        // 从 JSON 恢复时不执行任何操作
        ChangeProgress(-1, 1, "任务已过期，请重新下载");
        return Task.CompletedTask;
    }
}

