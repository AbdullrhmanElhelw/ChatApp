using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using ChatApp.Mobile.Features.Chat.Services;
using ChatApp.Mobile.Features.Common;

namespace ChatApp.Mobile.Platforms.Android;

[Service(Exported = false, ForegroundServiceType = ForegroundService.TypeDataSync)]
public class SignalRForegroundService : Service
{
    public const int ServiceNotificationId = 1001;
    public const string ServiceChannelId = "betweenus_service_channel";
    public const string MessagesChannelId = "betweenus_messages_channel";

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        EnsureChannels(this);

        var launchIntent = new Intent(this, typeof(MainActivity));
        launchIntent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
        var pendingFlags = Build.VERSION.SdkInt >= BuildVersionCodes.M
            ? PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
            : PendingIntentFlags.UpdateCurrent;
        var pendingIntent = PendingIntent.GetActivity(this, 0, launchIntent, pendingFlags);

        var notification = new NotificationCompat.Builder(this, ServiceChannelId)
            .SetContentTitle("BetweenUs")
            .SetContentText("متصل لاستقبال الرسائل الفورية")
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetOngoing(true)
            .SetSilent(true)
            .SetPriority((int)NotificationPriority.Low)
            .SetContentIntent(pendingIntent)
            .Build();

        if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
        {
            StartForeground(ServiceNotificationId, notification, ForegroundService.TypeDataSync);
        }
        else
        {
            StartForeground(ServiceNotificationId, notification);
        }

        _ = Task.Run(async () =>
        {
            try
            {
                var hub = IPlatformApplication.Current?.Services?.GetService<IChatHubService>();
                if (hub != null && !hub.IsConnected)
                {
                    await hub.ConnectAsync();
                }
            }
            catch { }
        });

        return StartCommandResult.Sticky;
    }

    public static void EnsureChannels(Context context)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;
        var manager = (NotificationManager?)context.GetSystemService(NotificationService);
        if (manager == null) return;

        if (manager.GetNotificationChannel(ServiceChannelId) == null)
        {
            var serviceChannel = new NotificationChannel(
                ServiceChannelId,
                "اتصال الخلفية",
                NotificationImportance.Low)
            {
                Description = "يبقي الاتصال مفتوحاً لاستقبال الرسائل بدون تأخير"
            };
            serviceChannel.SetShowBadge(false);
            manager.CreateNotificationChannel(serviceChannel);
        }

        if (manager.GetNotificationChannel(MessagesChannelId) == null)
        {
            var msgChannel = new NotificationChannel(
                MessagesChannelId,
                "الرسائل الجديدة",
                NotificationImportance.High)
            {
                Description = "تنبيهات فورية عند وصول رسالة جديدة"
            };
            msgChannel.EnableVibration(true);
            manager.CreateNotificationChannel(msgChannel);
        }
    }
}

public class AndroidLocalNotificationService : ILocalNotificationService
{
    private bool _started;

    public Task EnsureInitializedAndStartAsync()
    {
        try
        {
            var context = global::Android.App.Application.Context;
            SignalRForegroundService.EnsureChannels(context);

            if (!_started)
            {
                _started = true;
                var intent = new Intent(context, typeof(SignalRForegroundService));
                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                {
                    context.StartForegroundService(intent);
                }
                else
                {
                    context.StartService(intent);
                }
            }
        }
        catch { }

        return Task.CompletedTask;
    }

    public void ShowMessageNotification(string senderName, string messagePreview, Guid conversationId)
    {
        try
        {
            var context = global::Android.App.Application.Context;
            SignalRForegroundService.EnsureChannels(context);

            var launchIntent = new Intent(context, typeof(MainActivity));
            launchIntent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
            var pendingFlags = Build.VERSION.SdkInt >= BuildVersionCodes.M
                ? PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
                : PendingIntentFlags.UpdateCurrent;
            var pendingIntent = PendingIntent.GetActivity(context, conversationId.GetHashCode(), launchIntent, pendingFlags);

            var notification = new NotificationCompat.Builder(context, SignalRForegroundService.MessagesChannelId)
                .SetContentTitle(string.IsNullOrWhiteSpace(senderName) ? "رسالة جديدة" : senderName)
                .SetContentText(string.IsNullOrWhiteSpace(messagePreview) ? "لديك رسالة جديدة" : messagePreview)
                .SetSmallIcon(Resource.Mipmap.appicon)
                .SetAutoCancel(true)
                .SetPriority((int)NotificationPriority.High)
                .SetDefaults((int)NotificationDefaults.All)
                .SetContentIntent(pendingIntent)
                .Build();

            NotificationManagerCompat.From(context).Notify(conversationId.GetHashCode(), notification);
        }
        catch { }
    }
}
