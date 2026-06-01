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
}
