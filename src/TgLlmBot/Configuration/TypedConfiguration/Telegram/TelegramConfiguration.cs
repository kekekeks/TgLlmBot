using System;
using System.Collections.Generic;
using System.Linq;
using TgLlmBot.Configuration.Options.Telegram;

namespace TgLlmBot.Configuration.TypedConfiguration.Telegram;

public class TelegramConfiguration
{
    private const string DefaultCommandPrefix = "!";

    private TelegramConfiguration(
        string botToken,
        IReadOnlySet<long> allowedChatIds,
        string botName,
        string commandPrefix)
    {
        ArgumentNullException.ThrowIfNull(botToken);
        if (string.IsNullOrEmpty(botToken))
        {
            throw new ArgumentException("Value cannot be null or empty.", nameof(botToken));
        }

        ArgumentNullException.ThrowIfNull(allowedChatIds);
        if ((allowedChatIds.Count > 0) is not true)
        {
            throw new ArgumentException("Value should contain at least 1 element", nameof(allowedChatIds));
        }

        if (string.IsNullOrWhiteSpace(commandPrefix))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(commandPrefix));
        }

        BotToken = botToken;
        AllowedChatIds = allowedChatIds;
        BotName = botName;
        CommandPrefix = commandPrefix;
    }

    public string BotToken { get; }

    public IReadOnlySet<long> AllowedChatIds { get; }

    public string BotName { get; }

    public string CommandPrefix { get; }

    public static TelegramConfiguration Convert(TelegramOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var commandPrefix = string.IsNullOrWhiteSpace(options.CommandPrefix)
            ? DefaultCommandPrefix
            : options.CommandPrefix;
        return new(
            options.BotToken,
            options.AllowedChatIds.ToHashSet(),
            options.BotName,
            commandPrefix);
    }
}
