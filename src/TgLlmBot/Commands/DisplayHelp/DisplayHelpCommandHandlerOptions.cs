using System;

namespace TgLlmBot.Commands.DisplayHelp;

public class DisplayHelpCommandHandlerOptions
{
    public DisplayHelpCommandHandlerOptions(string botName, string commandPrefix)
    {
        if (string.IsNullOrWhiteSpace(botName))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(botName));
        }

        if (string.IsNullOrWhiteSpace(commandPrefix))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(commandPrefix));
        }

        BotName = botName;
        CommandPrefix = commandPrefix;
    }

    public string BotName { get; }

    public string CommandPrefix { get; }
}
