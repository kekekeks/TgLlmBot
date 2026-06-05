using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TgLlmBot.Configuration.Options.Llm;
using TgLlmBot.Services.DataAccess.TelegramMessages;

namespace TgLlmBot.Configuration.TypedConfiguration.Llm;

public class LlmConfiguration
{
    private LlmConfiguration(
        Uri endpoint,
        string apiKey,
        string model,
        IReadOnlyList<string> freeModels,
        string defaultResponse,
        string? systemPromptTemplate,
        ContextSelectionMode contextMode,
        int contextMaxMessages,
        int contextMaxCharacters)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentException("Value cannot be null or empty.", nameof(apiKey));
        }

        if (string.IsNullOrEmpty(model))
        {
            throw new ArgumentException("Value cannot be null or empty.", nameof(model));
        }

        if (string.IsNullOrEmpty(defaultResponse))
        {
            throw new ArgumentException("Value cannot be null or empty.", nameof(defaultResponse));
        }

        ArgumentNullException.ThrowIfNull(freeModels);

        Endpoint = endpoint;
        ApiKey = apiKey;
        Model = model;
        FreeModels = freeModels;
        DefaultResponse = defaultResponse;
        SystemPromptTemplate = systemPromptTemplate;
        ContextMode = contextMode;
        ContextMaxMessages = contextMaxMessages;
        ContextMaxCharacters = contextMaxCharacters;
    }

    public Uri Endpoint { get; }
    public string ApiKey { get; }
    public string Model { get; }

    // Models tried (in order) before falling back to Model when they are rate limited.
    public IReadOnlyList<string> FreeModels { get; }
    public string DefaultResponse { get; }

    // Contents of the configured system prompt file, or null to use the built-in default.
    public string? SystemPromptTemplate { get; }

    // How chat history is selected for LLM context.
    public ContextSelectionMode ContextMode { get; }

    // Caps on how much chat history is selected for context.
    public int ContextMaxMessages { get; }
    public int ContextMaxCharacters { get; }

    public static LlmConfiguration Convert(LlmOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var typedEndpoint))
        {
            throw new ArgumentException("Invalid endpoint.", nameof(options));
        }

        string? systemPromptTemplate = null;
        if (!string.IsNullOrWhiteSpace(options.SystemPromptPath))
        {
            if (!File.Exists(options.SystemPromptPath))
            {
                throw new FileNotFoundException(
                    $"The system prompt file {options.SystemPromptPath} was not found.",
                    options.SystemPromptPath);
            }

            systemPromptTemplate = File.ReadAllText(options.SystemPromptPath);
        }

        var contextMode = ContextSelectionMode.Full;
        if (!string.IsNullOrWhiteSpace(options.ContextMode)
            && (!Enum.TryParse(options.ContextMode, ignoreCase: true, out contextMode) || !Enum.IsDefined(contextMode)))
        {
            throw new ArgumentException($"Invalid context mode '{options.ContextMode}'.", nameof(options));
        }

        var freeModels = options.FreeModels is null
            ? Array.Empty<string>()
            : options.FreeModels
                .Where(static x => !string.IsNullOrWhiteSpace(x))
                .Select(static x => x.Trim())
                .ToArray();

        return new(
            typedEndpoint,
            options.ApiKey,
            options.Model,
            freeModels,
            options.DefaultResponse,
            systemPromptTemplate,
            contextMode,
            options.ContextMaxMessages ?? 300,
            options.ContextMaxCharacters ?? 50000);
    }
}
