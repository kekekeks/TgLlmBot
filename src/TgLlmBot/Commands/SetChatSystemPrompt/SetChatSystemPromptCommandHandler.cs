using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgLlmBot.CommandDispatcher.Abstractions;
using TgLlmBot.Services.DataAccess.SystemPrompts;
using TgLlmBot.Services.DataAccess.TelegramMessages;
using TgLlmBot.Services.Resources;

namespace TgLlmBot.Commands.SetChatSystemPrompt;

public class SetChatSystemPromptCommandHandler : AbstractCommandHandler<SetChatSystemPromptCommand>
{
    private readonly TelegramBotClient _bot;
    private readonly string _commandToken;
    private readonly ITelegramMessageStorage _storage;
    private readonly ISystemPromptService _systemPrompt;

    public SetChatSystemPromptCommandHandler(
        TelegramBotClient bot,
        ISystemPromptService systemPrompt,
        ITelegramMessageStorage storage,
        CommandPrefixOptions commandPrefixOptions)
    {
        ArgumentNullException.ThrowIfNull(bot);
        ArgumentNullException.ThrowIfNull(systemPrompt);
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(commandPrefixOptions);
        _bot = bot;
        _systemPrompt = systemPrompt;
        _storage = storage;
        _commandToken = $"{commandPrefixOptions.CommandPrefix}chat_role";
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public override async Task HandleAsync(SetChatSystemPromptCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var prompt = $"{command.Message.Text?.Trim()}".Trim();
            if (prompt.StartsWith(_commandToken, StringComparison.OrdinalIgnoreCase))
            {
                prompt = prompt[_commandToken.Length..].Trim();
            }

            var isAdmin = await IsAdminMessageAsync(command, cancellationToken);
            if (isAdmin)
            {
                await _systemPrompt.SetChatPromptAsync(command.Message.Chat.Id, prompt, cancellationToken);
                var response = await _bot.SendMessage(
                    command.Message.Chat,
                    "✅ Системный промпт чата успешно изменён",
                    ParseMode.MarkdownV2,
                    new()
                    {
                        MessageId = command.Message.MessageId
                    },
                    cancellationToken: cancellationToken);
                await _storage.StoreMessageAsync(response, command.Self, cancellationToken);
            }
            else
            {
                var response = await _bot.SendPhoto(
                    command.Message.Chat,
                    new InputFileStream(new MemoryStream(EmbeddedResources.NoJpg), "no.jpg"),
                    "❌ Только администраторы могут менять системный промпт чата",
                    ParseMode.MarkdownV2,
                    new()
                    {
                        MessageId = command.Message.MessageId
                    },
                    cancellationToken: cancellationToken);
                await _storage.StoreMessageAsync(response, command.Self, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            var response = await _bot.SendMessage(
                command.Message.Chat,
                ex.Message,
                ParseMode.None,
                new()
                {
                    MessageId = command.Message.MessageId
                },
                cancellationToken: cancellationToken);
            await _storage.StoreMessageAsync(response, command.Self, cancellationToken);
        }
    }

    private async Task<bool> IsAdminMessageAsync(SetChatSystemPromptCommand command, CancellationToken cancellationToken)
    {
        if (command.Message.Chat.Type is ChatType.Group or ChatType.Supergroup && command.Message.From is not null)
        {
            var admins = await _bot.GetChatAdministrators(command.Message.Chat, cancellationToken: cancellationToken);
            return admins.Any(x => x.User.Id == command.Message.From.Id);
        }

        return true;
    }
}
