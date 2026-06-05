using System;
using Microsoft.Extensions.AI;

namespace TgLlmBot.Services.OpenAIClient.Fallback;

// Pairs an IChatClient with the model name it was built for.
public sealed class NamedChatClient
{
    public NamedChatClient(string model, IChatClient client)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        ArgumentNullException.ThrowIfNull(client);
        Model = model;
        Client = client;
    }

    public string Model { get; }
    public IChatClient Client { get; }
}
