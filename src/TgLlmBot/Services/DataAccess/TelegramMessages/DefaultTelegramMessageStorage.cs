using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Telegram.Bot.Types;
using TgLlmBot.DataAccess;
using TgLlmBot.DataAccess.Models;
using TgLlmBot.Utils;

namespace TgLlmBot.Services.DataAccess.TelegramMessages;

public class DefaultTelegramMessageStorage : ITelegramMessageStorage
{
    private readonly DefaultTelegramMessageStorageOptions _options;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public DefaultTelegramMessageStorage(IServiceScopeFactory serviceScopeFactory, DefaultTelegramMessageStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(serviceScopeFactory);
        ArgumentNullException.ThrowIfNull(options);
        _serviceScopeFactory = serviceScopeFactory;
        _options = options;
    }

    [SuppressMessage("ReSharper", "ConvertToUsingDeclaration")]
    public async Task StoreMessageAsync(Message message, User self, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using (var asyncScope = _serviceScopeFactory.CreateAsyncScope())
        {
            var dbContext = asyncScope.ServiceProvider.GetRequiredService<BotDbContext>();
            await using (var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken))
            {
                var dbChatMessage = CreateDbChatMessage(message, self);
                dbContext.ChatHistory.Add(dbChatMessage);
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
        }
    }

    [SuppressMessage("ReSharper", "ConvertToUsingDeclaration")]
    [SuppressMessage("Usage", "CA2241:Provide correct arguments to formatting methods")]
    public async Task<DbChatMessage[]> SelectContextMessagesAsync(Message message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();
        var resultAccumulator = new List<DbChatMessage>();
        await using (var asyncScope = _serviceScopeFactory.CreateAsyncScope())
        {
            var dbContext = asyncScope.ServiceProvider.GetRequiredService<BotDbContext>();
            await using (var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken))
            {
                var messageId = new NpgsqlParameter($"{nameof(DbChatMessage.MessageId)}", message.MessageId);
                var chatId = new NpgsqlParameter($"{nameof(DbChatMessage.ChatId)}", message.Chat.Id);
                var botName = new NpgsqlParameter("BotName", _options.BotName);
                var includeAll = new NpgsqlParameter("IncludeAll", _options.ContextMode == ContextSelectionMode.Full);
                var maxMessages = new NpgsqlParameter("MaxMessages", _options.MaxContextMessages);
                var maxCharacters = new NpgsqlParameter("MaxCharacters", _options.MaxContextCharacters);
                var sql = FormattableStringFactory.Create(
                    $"""
                     WITH target_message AS (
                         SELECT "{nameof(DbChatMessage.Date)}" as cutoff_date
                         FROM public."{nameof(BotDbContext.ChatHistory)}"
                         WHERE "{nameof(DbChatMessage.MessageId)}" = @{nameof(DbChatMessage.MessageId)} AND "{nameof(DbChatMessage.ChatId)}" = @{nameof(DbChatMessage.ChatId)}
                     ),
                     candidates AS (
                         SELECT
                             ch."{nameof(DbChatMessage.Id)}",
                             ch."{nameof(DbChatMessage.MessageId)}",
                             ch."{nameof(DbChatMessage.ChatId)}",
                             ch."{nameof(DbChatMessage.MessageThreadId)}",
                             ch."{nameof(DbChatMessage.ReplyToMessageId)}",
                             ch."{nameof(DbChatMessage.Date)}",
                             ch."{nameof(DbChatMessage.FromUserId)}",
                             ch."{nameof(DbChatMessage.FromUsername)}",
                             ch."{nameof(DbChatMessage.FromFirstName)}",
                             ch."{nameof(DbChatMessage.FromLastName)}",
                             ch."{nameof(DbChatMessage.Text)}",
                             ch."{nameof(DbChatMessage.Caption)}",
                             ch."{nameof(DbChatMessage.IsLlmReplyToMessage)}"
                         FROM public."{nameof(BotDbContext.ChatHistory)}" ch
                         WHERE ch."{nameof(DbChatMessage.ChatId)}" = @{nameof(DbChatMessage.ChatId)}
                           AND ch."{nameof(DbChatMessage.Date)}" <= (SELECT cutoff_date FROM target_message)
                           AND ch."{nameof(DbChatMessage.MessageId)}" != @{nameof(DbChatMessage.MessageId)}
                           AND NOT EXISTS (
                                SELECT 1
                                FROM public."{nameof(BotDbContext.KickedUsers)}" k
                                WHERE k."{nameof(DbKickedUser.ChatId)}" = ch."{nameof(DbChatMessage.ChatId)}"
                                  AND k."{nameof(DbKickedUser.UserId)}" = ch."{nameof(DbChatMessage.FromUserId)}"
                           )
                     ),
                     mention_reply_targets AS (
                         SELECT "{nameof(DbChatMessage.ReplyToMessageId)}" AS target_message_id
                         FROM candidates
                         WHERE "{nameof(DbChatMessage.ReplyToMessageId)}" IS NOT NULL
                           AND (starts_with(LOWER(COALESCE("{nameof(DbChatMessage.Text)}", '')), LOWER(@BotName))
                                OR starts_with(LOWER(COALESCE("{nameof(DbChatMessage.Caption)}", '')), LOWER(@BotName)))
                     ),
                     bot_message_ids AS (
                         SELECT "{nameof(DbChatMessage.MessageId)}" AS bot_message_id
                         FROM candidates
                         WHERE "{nameof(DbChatMessage.IsLlmReplyToMessage)}" = TRUE
                     ),
                     filtered AS (
                         SELECT c.*
                         FROM candidates c
                         WHERE @IncludeAll
                            OR c."{nameof(DbChatMessage.IsLlmReplyToMessage)}" = TRUE
                            OR starts_with(LOWER(COALESCE(c."{nameof(DbChatMessage.Text)}", '')), LOWER(@BotName))
                            OR starts_with(LOWER(COALESCE(c."{nameof(DbChatMessage.Caption)}", '')), LOWER(@BotName))
                            OR c."{nameof(DbChatMessage.MessageId)}" IN (SELECT target_message_id FROM mention_reply_targets)
                            OR c."{nameof(DbChatMessage.ReplyToMessageId)}" IN (SELECT bot_message_id FROM bot_message_ids)
                     )
                     SELECT
                         "{nameof(DbChatMessage.Id)}",
                         "{nameof(DbChatMessage.MessageId)}",
                         "{nameof(DbChatMessage.ChatId)}",
                         "{nameof(DbChatMessage.MessageThreadId)}",
                         "{nameof(DbChatMessage.ReplyToMessageId)}",
                         "{nameof(DbChatMessage.Date)}",
                         "{nameof(DbChatMessage.FromUserId)}",
                         "{nameof(DbChatMessage.FromUsername)}",
                         "{nameof(DbChatMessage.FromFirstName)}",
                         "{nameof(DbChatMessage.FromLastName)}",
                         "{nameof(DbChatMessage.Text)}",
                         "{nameof(DbChatMessage.Caption)}",
                         "{nameof(DbChatMessage.IsLlmReplyToMessage)}"
                     FROM (
                              SELECT
                                  "{nameof(DbChatMessage.Id)}",
                                  "{nameof(DbChatMessage.MessageId)}",
                                  "{nameof(DbChatMessage.ChatId)}",
                                  "{nameof(DbChatMessage.MessageThreadId)}",
                                  "{nameof(DbChatMessage.ReplyToMessageId)}",
                                  "{nameof(DbChatMessage.Date)}",
                                  "{nameof(DbChatMessage.FromUserId)}",
                                  "{nameof(DbChatMessage.FromUsername)}",
                                  "{nameof(DbChatMessage.FromFirstName)}",
                                  "{nameof(DbChatMessage.FromLastName)}",
                                  "{nameof(DbChatMessage.Text)}",
                                  "{nameof(DbChatMessage.Caption)}",
                                  "{nameof(DbChatMessage.IsLlmReplyToMessage)}",
                                  SUM(COALESCE(LENGTH("{nameof(DbChatMessage.Text)}"), 0) + COALESCE(LENGTH("{nameof(DbChatMessage.Caption)}"), 0)) OVER (
                                      ORDER BY "{nameof(DbChatMessage.Date)}" DESC
                                      ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
                                      ) as cumulative_length
                              FROM filtered
                              ORDER BY "{nameof(DbChatMessage.Date)}" DESC
                              LIMIT @MaxMessages
                          ) as subquery
                     WHERE cumulative_length <= @MaxCharacters
                     ORDER BY "{nameof(DbChatMessage.Date)}" DESC;
                     """,
                    messageId,
                    chatId,
                    botName,
                    includeAll,
                    maxMessages,
                    maxCharacters);
                var dbResults = await dbContext.ChatHistory.FromSql(sql).AsNoTracking().ToListAsync(cancellationToken);
                resultAccumulator.AddRange(dbResults.OrderBy(x => x.Date));
                await transaction.CommitAsync(cancellationToken);
            }
        }

        return resultAccumulator.ToArray();
    }

    private static DbChatMessage CreateDbChatMessage(Message message, User self)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(self);
        var isSelfMessage = self.Id == message.From?.Id;
        return new(
            message.Id,
            message.Chat.Id,
            message.MessageThreadId,
            message.ReplyToMessage?.Id,
            message.Date,
            message.From?.Id,
            SurrogatePairSanitizer.SanitizeInvalidUtf16(message.From?.Username),
            SurrogatePairSanitizer.SanitizeInvalidUtf16(message.From?.FirstName),
            SurrogatePairSanitizer.SanitizeInvalidUtf16(message.From?.LastName),
            SurrogatePairSanitizer.SanitizeInvalidUtf16(message.Text),
            SurrogatePairSanitizer.SanitizeInvalidUtf16(message.Caption),
            isSelfMessage);
    }
}
