using System.Text.Json;
using SQLite;
using ChatApp.Mobile.Features.Chat.DTOs;
using ChatApp.Mobile.Features.Conversations.DTOs;

namespace ChatApp.Mobile.Features.Common;

public interface ILocalChatDatabase
{
    Task<List<ConversationSummaryDto>> GetConversationsAsync(Guid userId);
    Task SaveConversationsAsync(Guid userId, IEnumerable<ConversationSummaryDto> conversations);
    Task<List<MessageClientDto>> GetRecentMessagesAsync(Guid conversationId, int limit = 50);
    Task SaveMessagesAsync(Guid conversationId, IEnumerable<MessageClientDto> messages);
    Task UpsertMessageAsync(MessageClientDto message);
    Task<Guid?> GetLastSyncedMessageIdAsync(Guid conversationId);
}

[Table("Conversations")]
public class CachedConversationRow
{
    [PrimaryKey]
    public Guid Id { get; set; }

    [Indexed]
    public Guid UserId { get; set; }

    public int ConversationType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public Guid? OtherParticipantId { get; set; }
    public string? OtherParticipantUserName { get; set; }
    public bool IsOtherParticipantOnline { get; set; }
    public DateTime? OtherParticipantLastSeenAt { get; set; }
    public string? LastMessageJson { get; set; }
    public int UnreadCount { get; set; }
    public bool IsPinned { get; set; }
    public bool IsMuted { get; set; }
    public bool IsArchived { get; set; }
    public DateTime LastActivityAt { get; set; }
}

[Table("Messages")]
public class CachedMessageRow
{
    [PrimaryKey]
    public Guid Id { get; set; }

    [Indexed]
    public Guid ConversationId { get; set; }

    public Guid SenderId { get; set; }
    public string SenderUserName { get; set; } = string.Empty;
    public string SenderFullName { get; set; } = string.Empty;
    public string? SenderAvatarUrl { get; set; }
    public string? Content { get; set; }
    public int MessageType { get; set; }
    public Guid ClientMessageId { get; set; }

    [Indexed]
    public DateTime SentAt { get; set; }

    public DateTime? EditedAt { get; set; }
    public Guid? ReplyToMessageId { get; set; }
    public string? ReplyToContent { get; set; }
    public string? ReplyToSenderName { get; set; }
    public bool IsMine { get; set; }
    public int DeliveryStatus { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? ReactionsJson { get; set; }
}

[Table("MessageAttachments")]
public class CachedAttachmentRow
{
    [PrimaryKey]
    public Guid MessageId { get; set; }

    [Indexed]
    public Guid ConversationId { get; set; }

    public string? MediaUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public long? MediaSizeBytes { get; set; }
    public int MessageType { get; set; }
}

[Table("SyncState")]
public class ConversationSyncStateRow
{
    [PrimaryKey]
    public Guid ConversationId { get; set; }

    public Guid LastSyncedMessageId { get; set; }
    public DateTime LastSyncedAtUtc { get; set; }
}

public sealed class LocalChatDatabase : ILocalChatDatabase
{
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private SQLiteAsyncConnection? _db;
    private bool _initialized;

    private async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_initialized && _db != null)
        {
            return _db;
        }

        await _initLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!_initialized || _db == null)
            {
                SQLitePCL.Batteries_V2.Init();
                var dbPath = Path.Combine(FileSystem.AppDataDirectory, "betweenus_local.db3");
                _db = new SQLiteAsyncConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
                await _db.CreateTableAsync<CachedConversationRow>().ConfigureAwait(false);
                await _db.CreateTableAsync<CachedMessageRow>().ConfigureAwait(false);
                await _db.CreateTableAsync<CachedAttachmentRow>().ConfigureAwait(false);
                await _db.CreateTableAsync<ConversationSyncStateRow>().ConfigureAwait(false);
                _initialized = true;
            }
            return _db;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<List<ConversationSummaryDto>> GetConversationsAsync(Guid userId)
    {
        if (userId == Guid.Empty) return new List<ConversationSummaryDto>();
        try
        {
            var db = await GetConnectionAsync().ConfigureAwait(false);
            var rows = await db.Table<CachedConversationRow>()
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.LastActivityAt)
                .ToListAsync()
                .ConfigureAwait(false);

            return rows.Select(r => new ConversationSummaryDto(
                r.Id,
                (ConversationType)r.ConversationType,
                r.Title,
                r.AvatarUrl,
                r.OtherParticipantId,
                r.OtherParticipantUserName,
                r.IsOtherParticipantOnline,
                r.OtherParticipantLastSeenAt,
                string.IsNullOrWhiteSpace(r.LastMessageJson) ? null : JsonSerializer.Deserialize<LastMessageDto>(r.LastMessageJson),
                r.UnreadCount,
                r.IsPinned,
                r.IsMuted,
                r.IsArchived,
                r.LastActivityAt)).ToList();
        }
        catch
        {
            return new List<ConversationSummaryDto>();
        }
    }

    public async Task SaveConversationsAsync(Guid userId, IEnumerable<ConversationSummaryDto> conversations)
    {
        if (userId == Guid.Empty) return;
        try
        {
            var db = await GetConnectionAsync().ConfigureAwait(false);
            var rows = conversations.Select(c => new CachedConversationRow
            {
                Id = c.Id,
                UserId = userId,
                ConversationType = (int)c.ConversationType,
                Title = c.Title,
                AvatarUrl = c.AvatarUrl,
                OtherParticipantId = c.OtherParticipantId,
                OtherParticipantUserName = c.OtherParticipantUserName,
                IsOtherParticipantOnline = c.IsOtherParticipantOnline,
                OtherParticipantLastSeenAt = c.OtherParticipantLastSeenAt,
                LastMessageJson = c.LastMessage == null ? null : JsonSerializer.Serialize(c.LastMessage),
                UnreadCount = c.UnreadCount,
                IsPinned = c.IsPinned,
                IsMuted = c.IsMuted,
                IsArchived = c.IsArchived,
                LastActivityAt = c.LastActivityAt
            }).ToList();

            await db.RunInTransactionAsync(conn =>
            {
                conn.Execute("DELETE FROM Conversations WHERE UserId = ?", userId);
                foreach (var row in rows)
                {
                    conn.InsertOrReplace(row);
                }
            }).ConfigureAwait(false);
        }
        catch { }
    }

    public async Task<List<MessageClientDto>> GetRecentMessagesAsync(Guid conversationId, int limit = 50)
    {
        if (conversationId == Guid.Empty) return new List<MessageClientDto>();
        try
        {
            var db = await GetConnectionAsync().ConfigureAwait(false);
            var msgRows = await db.Table<CachedMessageRow>()
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.SentAt)
                .Take(limit)
                .ToListAsync()
                .ConfigureAwait(false);

            if (msgRows.Count == 0) return new List<MessageClientDto>();

            msgRows.Reverse();

            var attachments = await db.Table<CachedAttachmentRow>()
                .Where(a => a.ConversationId == conversationId)
                .ToListAsync()
                .ConfigureAwait(false);
            var attachmentMap = attachments.ToDictionary(a => a.MessageId);

            var result = new List<MessageClientDto>(msgRows.Count);
            foreach (var r in msgRows)
            {
                attachmentMap.TryGetValue(r.Id, out var att);
                List<ReactionCountClientDto>? reactions = null;
                if (!string.IsNullOrWhiteSpace(r.ReactionsJson))
                {
                    try { reactions = JsonSerializer.Deserialize<List<ReactionCountClientDto>>(r.ReactionsJson); }
                    catch { }
                }

                result.Add(new MessageClientDto(
                    r.Id,
                    r.ConversationId,
                    r.SenderId,
                    r.SenderUserName,
                    r.SenderFullName,
                    r.SenderAvatarUrl,
                    r.Content,
                    (MessageType)r.MessageType,
                    r.ClientMessageId,
                    r.SentAt,
                    r.EditedAt,
                    r.ReplyToMessageId,
                    r.ReplyToContent,
                    (MessageDeliveryStatus)r.DeliveryStatus,
                    r.DeliveredAt,
                    r.ReadAt,
                    r.IsMine,
                    att?.MediaUrl,
                    att?.ThumbnailUrl,
                    att?.MediaSizeBytes,
                    r.ReplyToSenderName,
                    reactions));
            }
            return result;
        }
        catch
        {
            return new List<MessageClientDto>();
        }
    }

    public async Task SaveMessagesAsync(Guid conversationId, IEnumerable<MessageClientDto> messages)
    {
        if (conversationId == Guid.Empty) return;
        try
        {
            var list = messages.Where(m => m.Id != Guid.Empty).ToList();
            if (list.Count == 0) return;

            var db = await GetConnectionAsync().ConfigureAwait(false);
            var msgRows = new List<CachedMessageRow>(list.Count);
            var attRows = new List<CachedAttachmentRow>();

            foreach (var m in list)
            {
                msgRows.Add(MapMessageRow(m));
                if (!string.IsNullOrWhiteSpace(m.MediaUrl) || !string.IsNullOrWhiteSpace(m.ThumbnailUrl))
                {
                    attRows.Add(MapAttachmentRow(m));
                }
            }

            var lastId = list[^1].Id;
            var syncRow = new ConversationSyncStateRow
            {
                ConversationId = conversationId,
                LastSyncedMessageId = lastId,
                LastSyncedAtUtc = DateTime.UtcNow
            };

            await db.RunInTransactionAsync(conn =>
            {
                foreach (var row in msgRows) conn.InsertOrReplace(row);
                foreach (var att in attRows) conn.InsertOrReplace(att);
                conn.InsertOrReplace(syncRow);
            }).ConfigureAwait(false);
        }
        catch { }
    }

    public async Task UpsertMessageAsync(MessageClientDto message)
    {
        if (message.Id == Guid.Empty || message.ConversationId == Guid.Empty) return;
        try
        {
            var db = await GetConnectionAsync().ConfigureAwait(false);
            var row = MapMessageRow(message);
            var hasAtt = !string.IsNullOrWhiteSpace(message.MediaUrl) || !string.IsNullOrWhiteSpace(message.ThumbnailUrl);
            var att = hasAtt ? MapAttachmentRow(message) : null;
            var syncRow = new ConversationSyncStateRow
            {
                ConversationId = message.ConversationId,
                LastSyncedMessageId = message.Id,
                LastSyncedAtUtc = DateTime.UtcNow
            };

            await db.RunInTransactionAsync(conn =>
            {
                conn.InsertOrReplace(row);
                if (att != null) conn.InsertOrReplace(att);
                conn.InsertOrReplace(syncRow);
            }).ConfigureAwait(false);
        }
        catch { }
    }

    public async Task<Guid?> GetLastSyncedMessageIdAsync(Guid conversationId)
    {
        if (conversationId == Guid.Empty) return null;
        try
        {
            var db = await GetConnectionAsync().ConfigureAwait(false);
            var row = await db.FindAsync<ConversationSyncStateRow>(conversationId).ConfigureAwait(false);
            return row?.LastSyncedMessageId;
        }
        catch
        {
            return null;
        }
    }

    private static CachedMessageRow MapMessageRow(MessageClientDto m) => new()
    {
        Id = m.Id,
        ConversationId = m.ConversationId,
        SenderId = m.SenderId,
        SenderUserName = m.SenderUserName,
        SenderFullName = m.SenderFullName,
        SenderAvatarUrl = m.SenderAvatarUrl,
        Content = m.Content,
        MessageType = (int)m.MessageType,
        ClientMessageId = m.ClientMessageId,
        SentAt = m.SentAt,
        EditedAt = m.EditedAt,
        ReplyToMessageId = m.ReplyToMessageId,
        ReplyToContent = m.ReplyToContent,
        ReplyToSenderName = m.ReplyToSenderName,
        IsMine = m.IsMine,
        DeliveryStatus = (int)m.DeliveryStatus,
        DeliveredAt = m.DeliveredAt,
        ReadAt = m.ReadAt,
        ReactionsJson = m.Reactions == null || m.Reactions.Count == 0 ? null : JsonSerializer.Serialize(m.Reactions)
    };

    private static CachedAttachmentRow MapAttachmentRow(MessageClientDto m) => new()
    {
        MessageId = m.Id,
        ConversationId = m.ConversationId,
        MediaUrl = m.MediaUrl,
        ThumbnailUrl = m.ThumbnailUrl,
        MediaSizeBytes = m.MediaSizeBytes,
        MessageType = (int)m.MessageType
    };
}
