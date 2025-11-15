using System;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;

namespace PotatoVN.App.Plugin.TuiHub.Services.Grpc;

public class GrpcChannelFactory
{
    private readonly string _librarianUrl;
    private readonly ClientTokenInterceptor _tokenInterceptor;
    private GrpcChannel? _channel;

    public GrpcChannelFactory(string librarianUrl, ClientTokenInterceptor tokenInterceptor)
    {
        _librarianUrl = librarianUrl;
        _tokenInterceptor = tokenInterceptor;
    }

    public GrpcChannel GetChannel()
    {
        if (_channel == null || _channel.State == ConnectivityState.Shutdown)
        {
            _channel = GrpcChannel.ForAddress(_librarianUrl, new GrpcChannelOptions
            {
                MaxReceiveMessageSize = 100 * 1024 * 1024, // 100 MB
                MaxSendMessageSize = 100 * 1024 * 1024
            });
        }
        return _channel;
    }

    public CallInvoker GetCallInvoker()
    {
        return GetChannel().Intercept(_tokenInterceptor);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _channel = null;
    }
}

