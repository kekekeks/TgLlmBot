using System;

namespace TgLlmBot.Commands.ChatWithLlm.Services;

public class DefaultLlmChatHandlerOptions
{
    public DefaultLlmChatHandlerOptions(string botName, string defaultResponse, string? systemPromptTemplate)
    {
        if (string.IsNullOrWhiteSpace(botName))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(botName));
        }

        if (string.IsNullOrWhiteSpace(defaultResponse))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(defaultResponse));
        }

        BotName = botName;
        DefaultResponse = defaultResponse;
        SystemPromptTemplate = systemPromptTemplate;
    }

    public string BotName { get; }

    public string DefaultResponse { get; }

    // Base system prompt loaded from a file, or null to use the built-in default.
    public string? SystemPromptTemplate { get; }
}
