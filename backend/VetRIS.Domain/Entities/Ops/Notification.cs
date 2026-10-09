using VetRIS.Domain.Common;
using VetRIS.Domain.Enums;

namespace VetRIS.Domain.Entities.Ops;

public class Notification : BaseEntity
{
    public string? RecipientUserId { get; private set; }
    public string? RecipientEmail { get; private set; }
    public string? RecipientPhone { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public string Type { get; private set; } = default!;
    public string Subject { get; private set; } = default!;
    public string Message { get; private set; } = default!;
    public NotificationStatus Status { get; private set; }
    public DateTime? SentAtUtc { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }
    public string? RelatedEntityType { get; private set; }
    public Guid? RelatedEntityId { get; private set; }

    private Notification()
    {
    } // EF Core

    public Notification(NotificationChannel channel, string type, string subject, string message,
        string? recipientUserId = null, string? recipientEmail = null, string? recipientPhone = null,
        string? relatedEntityType = null, Guid? relatedEntityId = null)
    {
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Notification message is required.", nameof(message));

        Channel = channel;
        Type = type;
        Subject = subject;
        Message = message;
        RecipientUserId = recipientUserId;
        RecipientEmail = recipientEmail;
        RecipientPhone = recipientPhone;
        RelatedEntityType = relatedEntityType;
        RelatedEntityId = relatedEntityId;
        Status = NotificationStatus.Pending;
    }

    public void MarkSent()
    {
        Status = NotificationStatus.Sent;
        SentAtUtc = DateTime.UtcNow;
    }

    public void MarkFailed() => Status = NotificationStatus.Failed;

    public void MarkRead()
    {
        Status = NotificationStatus.Read;
        ReadAtUtc = DateTime.UtcNow;
    }
}
