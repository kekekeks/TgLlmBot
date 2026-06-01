namespace TgLlmBot.Services.DataAccess.TelegramMessages;

public enum ContextSelectionMode
{
    // Entire chat history (bounded by the message-count and character limits).
    Full,

    // Only messages relevant to the bot: messages that start with the bot's name, the messages
    // those reply to, the bot's own messages, and follow-ups that reply to the bot's messages.
    MentionsOnly
}
