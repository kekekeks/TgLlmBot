using System;

namespace TgLlmBot.Commands;

// Shared by command handlers that strip the command token (prefix + name) from a message
// to extract its argument text.
public class CommandPrefixOptions
{
    public CommandPrefixOptions(string commandPrefix)
    {
        if (string.IsNullOrWhiteSpace(commandPrefix))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(commandPrefix));
        }

        CommandPrefix = commandPrefix;
    }

    public string CommandPrefix { get; }
}
