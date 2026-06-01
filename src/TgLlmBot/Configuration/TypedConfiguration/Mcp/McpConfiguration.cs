using TgLlmBot.Configuration.Options.Mcp;

namespace TgLlmBot.Configuration.TypedConfiguration.Mcp;

public class McpConfiguration
{
    private McpConfiguration(
        McpGithubConfiguration? github,
        McpBraveConfiguration? brave,
        McpContext7Configuration? context7)
    {
        Github = github;
        Brave = brave;
        Context7 = context7;
    }

    public McpGithubConfiguration? Github { get; }
    public McpBraveConfiguration? Brave { get; }
    public McpContext7Configuration? Context7 { get; }

    public static McpConfiguration Convert(McpOptions? options)
    {
        if (options is null)
        {
            return new(null, null, null);
        }

        var github = options.Github is null ? null : McpGithubConfiguration.Convert(options.Github);
        var brave = options.Brave is null ? null : McpBraveConfiguration.Convert(options.Brave);
        var context7 = options.Context7 is null ? null : McpContext7Configuration.Convert(options.Context7);
        return new(github, brave, context7);
    }
}
