using System.Data.Common;
using EduZim.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace EduZim.Infrastructure.Persistence.Interceptors;

public sealed class TenantConnectionInterceptor : DbConnectionInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantConnectionInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);

        var currentUser = _httpContextAccessor.HttpContext?.RequestServices.GetService<ICurrentUser>();
        var tenantId = currentUser?.TenantId;
        if (tenantId is null)
            return;

        if (connection is not NpgsqlConnection npg)
            return;

        await using var cmd = npg.CreateCommand();
        cmd.CommandText = "SELECT set_config('app.current_tenant_id', @tenant_id, false);";
        var p = cmd.CreateParameter();
        p.ParameterName = "tenant_id";
        p.Value = tenantId.Value.ToString();
        cmd.Parameters.Add(p);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
