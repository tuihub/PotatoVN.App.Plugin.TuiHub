using System;
using System.Threading;
using System.Threading.Tasks;
using GalgameManager.Models.BgTasks;

namespace PotatoVN.App.Plugin.TuiHub.BgTasks;

/// <summary>
/// 测试用后台任务1 - 取消必然失败
/// </summary>
public class DummyBgTask1 : BgTaskBase
{
    public override string Title => "测试任务1 (取消失败)";
    public override bool ProgressOnTrayIcon => false;
    
    /// <summary>
    /// 显示取消按钮
    /// </summary>
    public override bool CanCancel => true;

    /// <summary>
    /// 重写 TryCancel，始终返回 false，使取消失败
    /// </summary>
    public override bool TryCancel() => false;

    /// <summary>
    /// 重写 Cancel，抛出异常表示取消失败
    /// </summary>
    public override async Task CancelAsync()
    {
        await Task.Delay(2000); // 模拟一些工作
        throw new Exception("任务取消失败：Dummy");
    }

    protected override async Task RunInternal()
    {
        CancellationTokenSource = new CancellationTokenSource();
        var ct = CancellationToken!.Value;

        try
        {
            for (int i = 0; i <= 100; i++)
            {
                ct.ThrowIfCancellationRequested();
                ChangeProgress(i, 100, $"测试任务1进度: {i}%", false);
                
                if (i < 100)
                {
                    await Task.Delay(1000, ct);
                }
            }

            ChangeProgress(100, 100, "测试任务1已完成");
        }
        catch (OperationCanceledException)
        {
            ChangeProgress(-1, 1, "测试任务1已取消");
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
