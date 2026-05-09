namespace Diax.Domain.EmailMarketing.Enums;

public enum EmailEventType
{
    Sent = 0,
    Delivered = 1,
    Opened = 2,
    Clicked = 3,
    Bounced = 4,
    Spam = 5,
    Unsubscribed = 6,
    Failed = 7,
}
