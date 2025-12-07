using System;
using System.Threading;
using System.Threading.Tasks;
using GalgameManager.Models.BgTasks;

namespace PotatoVN.App.Plugin.TuiHub.BgTasks;

/// <summary>
/// 测试用后台任务2 - 可以正常取消
/// </summary>
public class DummyBgTask2 : BgTaskBase
{
    public override string Title => "测试任务2 (可取消)";
    public override bool ProgressOnTrayIcon => false;
    public override bool CanCancel => true;

    protected override async Task RunInternal()
    {
        CancellationTokenSource = new CancellationTokenSource();
        var ct = CancellationToken!.Value;

        try
        {
            for (int i = 0; i <= 100; i++)
            {
                ct.ThrowIfCancellationRequested();
                ChangeProgress(i, 100, $"测试任务2进度: {i}%", false);
                
                if (i < 100)
                {
                    await Task.Delay(1000, ct);
                }
            }

            ChangeProgress(100, 100, "测试任务2已完成");
        }
        catch (OperationCanceledException)
        {
            await Task.Delay(5000); // 模拟一些清理工作
            ChangeProgress(-1, 1, "测试任务2已取消");
        }
        finally
        {
            CancellationTokenSource?.Dispose();
            CancellationTokenSource = null;
        }
    }

    protected override Task RecoverFromJsonInternal()
    {
        ChangeProgress(-1, 1, "测试任务已过期");
        return Task.CompletedTask;
    }
}

