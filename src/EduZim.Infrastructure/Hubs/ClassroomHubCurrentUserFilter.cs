using EduZim.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace EduZim.Infrastructure.Hubs;

public sealed class ClassroomHubCurrentUserFilter : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        Bind(invocationContext.ServiceProvider, invocationContext.Context);
        return await next(invocationContext).ConfigureAwait(false);
    }

    public Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        Bind(context.ServiceProvider, context.Context);
        return next(context);
    }

    public Task OnDisconnectedAsync(
        HubLifetimeContext context,
        Exception? exception,
        Func<HubLifetimeContext, Exception?, Task> next)
    {
        Bind(context.ServiceProvider, context.Context);
        return next(context, exception);
    }

    private static void Bind(IServiceProvider services, HubCallerContext caller)
    {
        ICurrentUserInitializer initializer = services.GetRequiredService<ICurrentUserInitializer>();
        initializer.InitializeFromPrincipal(caller.User);
    }
}
