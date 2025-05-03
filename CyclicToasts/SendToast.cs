using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications; // Notifications library
using Microsoft.QueryStringDotNET; // QueryString.NET
using System.Diagnostics;

namespace CyclicToasts
{
    internal class SendToast
    {
        private const string _muteAudioFilePath = "ms-appx:///Assets/sound/mute.mp3";
        internal int Index { get; set; }
        internal string Title { get; set; }
        internal string Content { get; set; }
        internal bool IsMute { get; private set; }
        internal SendToast(int index, string title, string content, bool isMute)
        {
            Index = index;
            Title = title;
            Content = content;
            IsMute = isMute;
        }

        internal void Schedule(DateTimeOffset dateTimeOffset)
        {
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
                                Text = Title
                            },

                            new AdaptiveText()
                            {
                                Text = Content
                            }
                        }
                    }
                },

                Launch = new QueryString()
                {
                    { "index", Index.ToString() },
                    { "dateTime", dateTimeOffset.ToString("yyyy-MM-dd_HH:mm") }

                }.ToString()
            };

            if (IsMute)
                toastContent.Audio = new ToastAudio() { Src = new Uri(_muteAudioFilePath) };

            ScheduledToastNotification toast;
            if (dateTimeOffset <= DateTime.Now)
                return;
            try
            {
                toast = new ScheduledToastNotification(toastContent.GetXml(), dateTimeOffset);
            }
            catch (System.ArgumentException) // dateTimeが過去の場合
            {
                return;
            }
            toast.Tag = dateTimeOffset.ToString("yyyy-MM-dd_HH:mm");
            toast.Group = Index.ToString();

            try
            {
                ToastNotificationManager.CreateToastNotifier().AddToSchedule(toast);
            }
            catch (Exception)
            {
                // 時刻が過去である、または通知のスケジュール数が4096を超えた
                System.Diagnostics.Debug.WriteLine("Exception in AddToSchedule (SendToast.Schedule)");
                if (dateTimeOffset > DateTime.Now)
                {
                    // 通知のスケジュール数が4096を超えた場合の処理
                }
            }

            string s = "Toast is scheduled at: " + dateTimeOffset.ToString("s") + " (in BackgroundTask)";
            Debug.WriteLine(s);
        }
            
    }
}
