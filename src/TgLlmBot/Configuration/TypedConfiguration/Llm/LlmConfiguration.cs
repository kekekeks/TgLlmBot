using System;
using System.IO;
using TgLlmBot.Configuration.Options.Llm;

namespace TgLlmBot.Configuration.TypedConfiguration.Llm;

public class LlmConfiguration
{
    private LlmConfiguration(
        Uri endpoint,
        string apiKey,
        string model,
        string defaultResponse,
        string? systemPromptTemplate)
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

        Endpoint = endpoint;
        ApiKey = apiKey;
        Model = model;
        DefaultResponse = defaultResponse;
        SystemPromptTemplate = systemPromptTemplate;
    }

    public Uri Endpoint { get; }
    public string ApiKey { get; }
    public string Model { get; }
    public string DefaultResponse { get; }

    // Contents of the configured system prompt file, or null to use the built-in default.
    public string? SystemPromptTemplate { get; }

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

        return new(
            typedEndpoint,
            options.ApiKey,
            options.Model,
            options.DefaultResponse,
            systemPromptTemplate);
    }
}
