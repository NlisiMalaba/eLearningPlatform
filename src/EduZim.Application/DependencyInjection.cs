using System.Reflection;
using EduZim.Application.Common.Behaviours;
using EduZim.Application.LiveClassrooms.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace EduZim.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(Assembly.GetExecutingAssembly());
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TenantScopeBehaviour<,>));
        services.AddSingleton<IClassroomPresenceTracker, ClassroomPresenceTracker>();
        services.AddScoped<IClassroomRealtimeService, ClassroomRealtimeService>();
        return services;
    }
}
