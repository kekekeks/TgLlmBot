using System.ComponentModel.DataAnnotations;

namespace TgLlmBot.Configuration.Options.Llm;

public class LlmOptions
{
    [Required]
    [MaxLength(10000)]
    [Url]
    public string Endpoint { get; set; } = default!;

    [Required]
    [MaxLength(10000)]
    public string ApiKey { get; set; } = default!;

    [Required]
    [MaxLength(10000)]
    public string Model { get; set; } = default!;

    [Required]
    [MaxLength(10000)]
    public string DefaultResponse { get; set; } = default!;

    // Optional: path to a .txt file holding the base system prompt.
    // Supports the {BotName} and {CurrentDateUtc} placeholders.
    // When empty, the built-in default prompt is used.
    [MaxLength(10000)]
    public string? SystemPromptPath { get; set; }

    // Optional: how chat history is selected for LLM context.
    // "Full" (default) - entire history; "MentionsOnly" - only messages starting with the bot's
    // name, the messages those reply to, the bot's own messages, and follow-ups replying to the bot.
    [MaxLength(100)]
    public string? ContextMode { get; set; }

    // Optional caps on how much chat history is selected for context. Defaults: 300 / 50000.
    [Range(1, int.MaxValue)]
    public int? ContextMaxMessages { get; set; }

    [Range(1, int.MaxValue)]
    public int? ContextMaxCharacters { get; set; }
}
