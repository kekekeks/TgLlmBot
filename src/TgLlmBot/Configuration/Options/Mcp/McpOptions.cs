namespace TgLlmBot.Configuration.Options.Mcp;

public class McpOptions
{
    // Each server is optional: omit its section to skip launching it and registering its tools.
    public McpGithubOptions? Github { get; set; }

    public McpBraveOptions? Brave { get; set; }

    public McpContext7Options? Context7 { get; set; }
}
