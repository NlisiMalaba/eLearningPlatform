using EduZim.Application.Common.Interfaces;
using EduZim.Application.ZimBot.Services;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace EduZim.Infrastructure.Ai;

internal sealed class SemanticKernelAiService : IAiService
{
    private readonly AzureOpenAiKernelAccessor _kernelAccessor;
    private readonly ResiliencePipeline _pipeline;
    private readonly ILogger<SemanticKernelAiService> _logger;

    public SemanticKernelAiService(
        AzureOpenAiKernelAccessor kernelAccessor,
        ResiliencePipeline pipeline,
        ILogger<SemanticKernelAiService> logger)
    {
        _kernelAccessor = kernelAccessor;
        _pipeline = pipeline;
        _logger = logger;
    }

    public async Task<string> ChatAsync(string systemPrompt, string userMessage, CancellationToken ct = default)
    {
        Kernel? kernel = _kernelAccessor.Kernel;
        if (kernel is null)
            return Fallback("Azure OpenAI is not configured.");

        try
        {
            return await _pipeline
                .ExecuteAsync(token => CompleteAsync(kernel, systemPrompt, userMessage, token), ct)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is BrokenCircuitException or TimeoutRejectedException or HttpRequestException)
        {
            return Fallback("AI service is unavailable.", ex);
        }
        catch (Exception ex)
        {
            return Fallback("AI completion failed.", ex);
        }
    }

    private static async ValueTask<string> CompleteAsync(
        Kernel kernel,
        string systemPrompt,
        string userMessage,
        CancellationToken ct)
    {
        IChatCompletionService chat = kernel.GetRequiredService<IChatCompletionService>();
        ChatHistory history = new(systemPrompt);
        history.AddUserMessage(userMessage);
        ChatMessageContent result = await chat.GetChatMessageContentAsync(history, cancellationToken: ct)
            .ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(result.Content) ? string.Empty : result.Content;
    }

    private string Fallback(string reason, Exception? exception = null)
    {
        if (exception is null)
            _logger.LogWarning("ZimBot returning fallback: {Reason}", reason);
        else
            _logger.LogError(exception, "ZimBot returning fallback: {Reason}", reason);

        return ZimBotMessages.Unavailable;
    }
}
