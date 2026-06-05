using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace TgLlmBot.Services.OpenAIClient.Fallback;

// An IChatClient that tries a list of (free) models in order, falling back to the main model when
// every free model is rate limited (HTTP 429). The returned response's ModelId reflects the model
// that actually served the request, so callers can tell which one answered.
public sealed partial class FallbackChatClient : IChatClient
{
    private readonly IReadOnlyList<NamedChatClient> _freeClients;
    private readonly IChatClient _mainClient;
    private readonly ILogger<FallbackChatClient> _logger;

    public FallbackChatClient(
        IReadOnlyList<NamedChatClient> freeClients,
        IChatClient mainClient,
        ILogger<FallbackChatClient> logger)
    {
        ArgumentNullException.ThrowIfNull(freeClients);
        ArgumentNullException.ThrowIfNull(mainClient);
        ArgumentNullException.ThrowIfNull(logger);
        _freeClients = freeClients;
        _mainClient = mainClient;
        _logger = logger;
    }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        foreach (var free in _freeClients)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var response = await free.Client.GetResponseAsync(messages, options, cancellationToken);
                response.ModelId = free.Model;
                return response;
            }
            catch (Exception ex) when (IsRateLimited(ex))
            {
                Log.FreeModelRateLimited(_logger, free.Model);
            }
        }

        return await _mainClient.GetResponseAsync(messages, options, cancellationToken);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var free in _freeClients)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var enumerator = free.Client.GetStreamingResponseAsync(messages, options, cancellationToken).GetAsyncEnumerator(cancellationToken);
            try
            {
                // Probe the first update so a rate-limit at the start of the stream can fall through
                // to the next model. Once the first chunk has been yielded, errors propagate.
                bool hasNext;
                try
                {
                    hasNext = await enumerator.MoveNextAsync();
                }
                catch (Exception ex) when (IsRateLimited(ex))
                {
                    Log.FreeModelRateLimited(_logger, free.Model);
                    continue;
                }

                while (hasNext)
                {
                    var update = enumerator.Current;
                    update.ModelId = free.Model;
                    yield return update;
                    hasNext = await enumerator.MoveNextAsync();
                }

                yield break;
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
        }

        await foreach (var update in _mainClient.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            yield return update;
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        return _mainClient.GetService(serviceType, serviceKey);
    }

    public void Dispose()
    {
        foreach (var free in _freeClients)
        {
            free.Client.Dispose();
        }

        _mainClient.Dispose();
    }

    private static bool IsRateLimited(Exception ex)
    {
        return ex switch
        {
            ClientResultException clientResultException => clientResultException.Status == (int)HttpStatusCode.TooManyRequests,
            HttpRequestException httpRequestException => httpRequestException.StatusCode == HttpStatusCode.TooManyRequests,
            _ => false
        };
    }

    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes")]
    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Free model {Model} is rate limited, trying the next one")]
        public static partial void FreeModelRateLimited(ILogger logger, string model);
    }
}
