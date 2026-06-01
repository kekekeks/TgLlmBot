using System;

namespace TgLlmBot.Services.DataAccess.TelegramMessages;

public class DefaultTelegramMessageStorageOptions
{
    public DefaultTelegramMessageStorageOptions(
        ContextSelectionMode contextMode,
        string botName,
        int maxContextMessages,
        int maxContextCharacters)
    {
        if (string.IsNullOrWhiteSpace(botName))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(botName));
        }

        if (maxContextMessages <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxContextMessages), maxContextMessages, "Value must be positive.");
        }

        if (maxContextCharacters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxContextCharacters), maxContextCharacters, "Value must be positive.");
        }

        ContextMode = contextMode;
        BotName = botName;
        MaxContextMessages = maxContextMessages;
        MaxContextCharacters = maxContextCharacters;
    }

    public ContextSelectionMode ContextMode { get; }

    // Used to match "mentions" of the bot by name (StartsWith) when ContextMode is MentionsOnly.
    public string BotName { get; }

    // Caps on how much chat history is selected for LLM context.
    public int MaxContextMessages { get; }

    public int MaxContextCharacters { get; }
}
