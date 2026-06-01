using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TgLlmBot.CommandDispatcher.Abstractions;
using TgLlmBot.Services.Telegram.Markdown;

namespace TgLlmBot.Commands.DisplayHelp;

public class DisplayHelpCommandHandler : AbstractCommandHandler<DisplayHelpCommand>
{
    private readonly TelegramBotClient _bot;
    private readonly string _response;

    public DisplayHelpCommandHandler(
        DisplayHelpCommandHandlerOptions options,
        TelegramBotClient bot,
        ITelegramMarkdownConverter markdownConverter)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(bot);
        ArgumentNullException.ThrowIfNull(markdownConverter);
        _response = BuildHelpTemplate(markdownConverter, options.BotName, options.CommandPrefix);
        _bot = bot;
    }

    public override async Task HandleAsync(DisplayHelpCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(command);
        await _bot.SendMessage(
            command.Message.Chat,
            _response,
            ParseMode.MarkdownV2,
            new()
            {
                MessageId = command.Message.MessageId
            },
            cancellationToken: cancellationToken);
    }

    private static string BuildHelpTemplate(ITelegramMarkdownConverter markdownConverter, string botName, string commandPrefix)
    {
        var p = commandPrefix;
        var builder = new StringBuilder();
        builder.Append('`').Append(botName).Append('`').AppendLine(" - префикс для того чтобы задать вопрос LLM");
        builder.AppendLine();
        builder.AppendLine("Например:");
        builder.Append('`').Append(botName).Append(" напиши Hello World на C#").Append('`').AppendLine();
        builder.AppendLine();
        builder.AppendLine();
        builder.AppendLine("Список команд:");
        builder.AppendLine();
        builder.AppendLine(CultureInfo.InvariantCulture, $"* `{p}ping` - проверка работоспособности бота");
        builder.AppendLine(CultureInfo.InvariantCulture, $"* `{p}model` - отображает текущую используемую LLM и endpoint к которому идут обращения");
        builder.AppendLine(CultureInfo.InvariantCulture, $"* `{p}repo` - ссылка на GitHub репозиторий с исходным кодом бота");
        builder.AppendLine(CultureInfo.InvariantCulture, $"* `{p}usage` - статистика использования API ключа");
        builder.AppendLine(CultureInfo.InvariantCulture, $"* `{p}rating` - измеряет уровень кринжа и показывает рейтинг щитпостеров");
        builder.AppendLine(CultureInfo.InvariantCulture, $"* `{p}chat_role` - модифицирует системный промпт по-умолчанию (для всего чата) произвольным образом (например: `{p}chat_role общайся на древнерусском`)");
        builder.AppendLine(CultureInfo.InvariantCulture, $"* `{p}chat_role_reset` - сбрасывает системный промпт по-умолчанию (для всего чата) на стандартный");
        builder.AppendLine(CultureInfo.InvariantCulture, $"* `{p}chat_role_show` - показывает текущий системный промпт по-умолчанию (для всего чата)");
        builder.AppendLine(CultureInfo.InvariantCulture, $"* `{p}personal_role` - модифицирует системный промпт для общения конкретно с тобой произвольным образом (например: `{p}personal_role отвечай кратко`). Имеет приоритет над `{p}chat_role`");
        builder.AppendLine(CultureInfo.InvariantCulture, $"* `{p}personal_role_reset` - сбрасывает системный промпт для общения конкретно с тобой на стандартный");
        builder.AppendLine(CultureInfo.InvariantCulture, $"* `{p}personal_role_show` - показывает текущий системный для общения конкретно с тобой");
        builder.AppendLine(
            CultureInfo.InvariantCulture,
            $"* `{p}set_limit` - устанавливает пользователю лимит на общение с LLM (для этого нужно отправить эту команду реплаем на сообщение того, кому нужно установить лимит и указать количество сообщений, которое будет ему доступно в день; например: `{p}set_limit 5`)");
        var rawMarkdown = builder.ToString();
        var optimizedMarkdown = markdownConverter.ConvertToSolidTelegramMarkdown(rawMarkdown);
        return optimizedMarkdown;
    }
}
