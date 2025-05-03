using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications; // Notifications library
using Microsoft.QueryStringDotNET; // QueryString.NET
using System.Diagnostics;

namespace Simple_Notification
{
    static class SendToast
    {
        private const string _muteAudioFilePath = "ms-appx:///Assets/sound/mute.mp3";

        /* 今すぐトーストを通知する */
        public static void Send(MyToastContent myToastContent)
        {
            var toastContent = CreateToastContent(myToastContent, null);
            var toast = new ToastNotification(toastContent.GetXml());
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }
        /* 与えられた時刻にスケジュールする。スケジュールに成功すればtrue, 失敗すればfalseを返す */
        public static bool Schedule(MyToastContent myToastContent, DateTimeOffset dateTimeOffset)
        {
            if (myToastContent == null)
                throw new ArgumentNullException();
            var toastContent = CreateToastContent(myToastContent, dateTimeOffset);
            if (toastContent == null)
            {
                Debug.WriteLine("toastContentがnullです（SendToast.Schedule）");
                return false;
            }

            ScheduledToastNotification toast;
            if (dateTimeOffset <= DateTimeOffset.Now)
            {
                return false;
            }
            try
            {
                toast = new ScheduledToastNotification(toastContent.GetXml(), dateTimeOffset);
            }
            //dateTimeOffsetが過去の場合
            catch (ArgumentException)
            {
                Debug.WriteLine("System.ArgumentException in SendToast.Schedule");
                return false;
            }
            toast.Tag = myToastContent.GetToastTag(dateTimeOffset);
            toast.Group = myToastContent.ToastGroup();
            try
            {
                ToastNotificationManager.CreateToastNotifier().AddToSchedule(toast);
                string s = "Toast is scheduled at: " + dateTimeOffset.ToString("s") + "in Simple_Notification.SendToast.Schedule)";
                Debug.WriteLine(s);
            }
            catch (Exception)
            {
                // 時刻が過去である、または通知のスケジュール数が4096を超えた
                Debug.WriteLine("Exception in AddToSchedule (SendToast.Schedule)");
                return false;
            }
            return true;
        }
        private static ToastContent CreateToastContent(MyToastContent myToastContent, DateTimeOffset? dateTime)
        {
            if (myToastContent == null)
                throw new ArgumentNullException();
            if (myToastContent.Options == null)
            {
                Debug.WriteLine("myToastContent.Options が nullです(SendToast.CreateToastContent)");
                return null;
            }
            var toastContent = new ToastContent()
            {
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = myToastContent.Options.Title
                            },

                            new AdaptiveText()
                            {
                                Text = myToastContent.Options.Content
                            }
                        }
                    }
                },
                Launch = new QueryString()
                {
                    { "index", myToastContent.Index.ToString() },
                    { "dateTimeoffset", dateTime?.ToString("u") ?? "" }

                }.ToString()
            };
            if (myToastContent.Options.IsMute)
                toastContent.Audio = new ToastAudio() { Src = new Uri(_muteAudioFilePath) };
            return toastContent;
        }
        public static void CancelAll(MyToastContent myToastContent)
        {
            /* 参考URL: https://docs.microsoft.com/ja-jp/windows/uwp/design/shell/tiles-and-notifications/scheduled-toast */
            var notifier = ToastNotificationManager.CreateToastNotifier();
            IReadOnlyList<ScheduledToastNotification> scheduledToasts = notifier.GetScheduledToastNotifications();

            var toRemove = scheduledToasts.Where(i => i.Group == myToastContent.ToastGroup());

            foreach (var item in toRemove)
                notifier.RemoveFromSchedule(item);
        } 
    }
}
