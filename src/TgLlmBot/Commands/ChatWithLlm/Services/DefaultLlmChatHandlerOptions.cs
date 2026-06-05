using System;
using System.Collections.Generic;

namespace TgLlmBot.Commands.ChatWithLlm.Services;

public class DefaultLlmChatHandlerOptions
{
    public DefaultLlmChatHandlerOptions(
        string botName,
        string defaultResponse,
        string? systemPromptTemplate,
        IEnumerable<string> freeModels)
    {
        if (string.IsNullOrWhiteSpace(botName))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(botName));
        }

        if (string.IsNullOrWhiteSpace(defaultResponse))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(defaultResponse));
        }

        ArgumentNullException.ThrowIfNull(freeModels);

        BotName = botName;
        DefaultResponse = defaultResponse;
        SystemPromptTemplate = systemPromptTemplate;
        FreeModels = new HashSet<string>(freeModels, StringComparer.OrdinalIgnoreCase);
    }

    public string BotName { get; }

    public string DefaultResponse { get; }

    // Base system prompt loaded from a file, or null to use the built-in default.
    public string? SystemPromptTemplate { get; }

    // Models served for free; when the response came from one of these, its name is shown
    // instead of a cost estimate.
    public IReadOnlySet<string> FreeModels { get; }
}
