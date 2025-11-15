using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using PotatoVN.App.Plugin.TuiHub.Services.Auth;

namespace PotatoVN.App.Plugin.TuiHub.Services.Grpc;

public class ClientTokenInterceptor : Interceptor
{
    private readonly TuiHubAuthService _authService;

    public ClientTokenInterceptor(TuiHubAuthService authService)
    {
        _authService = authService;
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var ct = context.Options.CancellationToken;
        
        try
        {
            // 检查是否需要刷新 Token
            if (!_authService.IsAccessTokenValid())
            {
                if (_authService.HasRefreshToken())
                {
                    // 使用 RefreshToken 刷新
                    var refreshTask = _authService.RefreshTokenAsync(ct);
                    refreshTask.Wait(ct);
                }
                else
                {
                    throw new RpcException(new Status(StatusCode.Unauthenticated, "No valid token available"));
                }
            }

            return ContinueWithAccessToken(request, context, continuation);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unauthenticated)
        {
            // Token 失效，尝试刷新一次
            if (_authService.HasRefreshToken())
            {
                try
                {
                    var refreshTask = _authService.RefreshTokenAsync(ct);
                    refreshTask.Wait(ct);
                    return ContinueWithAccessToken(request, context, continuation);
                }
                catch
                {
                    // 刷新失败，清除 Token
                    _authService.ClearTokens();
                    throw;
                }
            }
            throw;
        }
    }

    public override TResponse BlockingUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        throw new NotImplementedException("BlockingUnaryCall is not supported");
    }

    private AsyncUnaryCall<TResponse> ContinueWithAccessToken<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        where TRequest : class
        where TResponse : class
    {
        var metadata = context.Options.Headers ?? new Metadata();
        
        // 移除旧的 Authorization header
        var authMetadata = metadata.GetValue("authorization");
        if (authMetadata != null)
        {
            var index = -1;
            for (int i = 0; i < metadata.Count; i++)
            {
                if (metadata[i].Key.Equals("authorization", StringComparison.OrdinalIgnoreCase))
                {
                    index = i;
                    break;
                }
            }
            if (index >= 0)
            {
                metadata.RemoveAt(index);
            }
        }

        // 添加新的 Authorization header
        var accessToken = _authService.GetAccessToken();
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            metadata.Add("Authorization", $"Bearer {accessToken}");
        }

        var newOptions = context.Options.WithHeaders(metadata);
        var newContext = new ClientInterceptorContext<TRequest, TResponse>(
            context.Method, context.Host, newOptions);

        return base.AsyncUnaryCall(request, newContext, continuation);
    }
}

