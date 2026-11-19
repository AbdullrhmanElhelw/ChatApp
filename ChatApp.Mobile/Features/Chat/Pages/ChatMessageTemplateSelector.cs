using ChatApp.Mobile.Features.Chat.DTOs;

namespace ChatApp.Mobile.Features.Chat.Pages;

public class ChatMessageTemplateSelector : DataTemplateSelector
{
    public DataTemplate? OutgoingTextTemplate { get; set; }
    public DataTemplate? IncomingTextTemplate { get; set; }
    public DataTemplate? StickerTemplate { get; set; }
    public DataTemplate? RichMessageTemplate { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        if (item is not ChatMessageItem msg)
        {
            return RichMessageTemplate!;
        }

        if (msg.IsSticker && StickerTemplate != null)
        {
            return StickerTemplate;
        }

        if (msg.IsTextMessage && !msg.HasReplyTo && !msg.HasReactions)
        {
            if (msg.IsMine && OutgoingTextTemplate != null)
            {
                return OutgoingTextTemplate;
            }
            if (!msg.IsMine && IncomingTextTemplate != null)
            {
                return IncomingTextTemplate;
            }
        }

        return RichMessageTemplate!;
    }
}
