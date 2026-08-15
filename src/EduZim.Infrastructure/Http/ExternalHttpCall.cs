using Polly.CircuitBreaker;
using Polly.Timeout;

namespace EduZim.Infrastructure.Http;

internal static class ExternalHttpCall
{
    public static bool HasBaseAddress(HttpClient client) => client.BaseAddress is not null;

    public static bool IsTransient(Exception exception) =>
        exception is HttpRequestException
            or TimeoutRejectedException
            or BrokenCircuitException
            or TaskCanceledException;
}
