using System;
using Markdig;
using Markdig.Extensions.EmphasisExtras;
using TgLlmBot.Services.Telegram.Markdown.Internal;

namespace TgLlmBot.Services.Telegram.Markdown;

/// <summary>
///     Converts Markdown to Telegram MarkdownV2 format with proper escaping.
/// </summary>
public class DefaultTelegramMarkdownConverter : ITelegramMarkdownConverter
{
    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
        .UseSpoilers()
        .UseAlertBlocks()
        .UseAutoIdentifiers()
        .UseCustomContainers()
        .UseDefinitionLists()
        .UseEmphasisExtras(EmphasisExtraOptions.Strikethrough)
        .UseGridTables()
        .UseMediaLinks()
        .UsePipeTables()
        .UseListExtras()
        .UseTaskLists()
        .UseAutoLinks()
        .UseGenericAttributes() // Must be last as it is one parser that is modifying other parsers
        .Build();

    public string[] ConvertToPartedTelegramMarkdown(string normalMarkdown, int partLengthLimit)
    {
        var document = Markdig.Markdown.Parse(normalMarkdown, MarkdownPipeline);
        var parts = new TelegramMarkdownRenderer().RenderParted(document, partLengthLimit);
        return parts;
    }

    public string ConvertToSolidTelegramMarkdown(string normalMarkdown)
    {
        var document = Markdig.Markdown.Parse(normalMarkdown, MarkdownPipeline);
        var result = new TelegramMarkdownRenderer().Render(document);
        return result;
    }
}
