using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Popups;
using System.Runtime.Serialization.Formatters.Binary;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications; // Notifications library
using Microsoft.QueryStringDotNET; // QueryString.NET
using Windows.ApplicationModel.Background;
using System.Diagnostics;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Simple_Notification
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class DebugPage : Page
    {
        public DebugPage()
        {
            this.InitializeComponent();
        }

        private void ToHomeButton_Click(object sender, RoutedEventArgs e) => this.Frame.Navigate(typeof(MainPage));

        // バックグラウンドタスクが登録されているかを表示
        private void BackgroundCheckButton_Click(object sender, RoutedEventArgs e)
        {
            bool taskRegistered = false;
            if (BackgroundTaskName.Text == "")
            {
                Background_Message.Text = "";
                taskRegistered = RegisterBackground.IsBackgroundRegistered();
                if (taskRegistered)
                    Background_Message.Text = "タスク名CyclicToastsは登録されています";
                else
                    Background_Message.Text = "タスク名CyclicToastsは登録されていません";
            }
            else
            {
                foreach (var task in BackgroundTaskRegistration.AllTasks)
                {
                    if (task.Value.Name == BackgroundTaskName.Text)
                    {
                        taskRegistered = true;
                        break;
                    }
                }
                if (taskRegistered)
                    Background_Message.Text = "タスク名" + BackgroundTaskName.Text + "は登録されています";
                else
                    Background_Message.Text = "タスク名" + BackgroundTaskName.Text + "は登録されていません";
            }
        }
        // TextBoxのタスク名のバックグラウンドタスクを登録解除
        private async void BackgroundUnregisterButton_Click(object sender, RoutedEventArgs e)
        {
            if (BackgroundTaskName.Text == "")
            {
                Background_Message.Text = "タスク名を入力してください。";
                return;
            }
            // 参考URL: https://stackoverflow.com/questions/43154770/unregister-uwp-background-task
            var tasks = BackgroundTaskRegistration.AllTasks;
            foreach (var task in tasks)
            {
                // You can check here for the name
                string name = task.Value.Name;
                if (name == BackgroundTaskName.Text)
                {
                    task.Value.Unregister(true);
                    Background_Message.Text = "タスク名" + BackgroundTaskName.Text + "は登録解除されました";
                }
            }
        }
        // バックグラウンドタスク(CyclicToasts)が必要で、まだ登録されていなければ登録
        private async void BackgroundButton_Click(object sender, RoutedEventArgs e)
        {
            if (RegisterBackground.IsBackgroundTaskNeeded() && RegisterBackground.IsBackgroundRegistered())
            {
                await BackgroundExecutionManager.RequestAccessAsync();
                RegisterBackground.Register();
                GeneralMessageBlock.Text = "タスク名CyclickToastsが登録されました";
            }
            else
                GeneralMessageBlock.Text = "タスク名CyclickToastsは（その必要がなかったため）登録されませんでした";
        }

        private async void SaveXmlButton_Click(object sender, RoutedEventArgs e)
        {
            await App.GetMyToastContents().SaveToasts();
        }

        private async void LoadXmlButton_Click(object sender, RoutedEventArgs e)
        {
            await App.GetMyToastContents().LoadToasts();
        }

        private void RegisterBackgroundTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var taskRegistered = false;
            var backgroundTaskName = "CyclicToasts";
            var backgroundTaskEntryPoint = "CyclicToasts.MyBackgroundTask";
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == backgroundTaskName)
                {
                    taskRegistered = true;
                    break;
                }
            }
            if (taskRegistered)
                return;

            var builder = new BackgroundTaskBuilder
            {
                Name = backgroundTaskName,
                TaskEntryPoint = backgroundTaskEntryPoint
            };
            builder.SetTrigger(new TimeTrigger(freshnessTime: 15, oneShot: false));
            _ = builder.Register();
        }

        private void UnregisterBackgroundTaskButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var task in BackgroundTaskRegistration.AllTasks)
                task.Value.Unregister(true);
        }

        private void AnotherPopupButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.Assert(image is FrameworkElement);
            try
            {
                FlyoutBase.ShowAttachedFlyout((FrameworkElement)image);
            }
            catch (System.ArgumentException ex)
            {
                System.Diagnostics.Debug.Fail(ex.Message);
            }
        }

        private void StringButton_Click(object sender, RoutedEventArgs e)
        {
            string s = "Toast is scheduled at: " + DateTime.Now.ToString("s") + " (in BackgroundTask)";
            Debug.WriteLine(s);
        }

        private void LinqDistinct_Click(object sender, RoutedEventArgs e)
        {
            List<Test> tests = new List<Test>();
            var test1 = new Test(1);
            var test1_1 = new Test(1);
            var test2 = new Test(2);
            var test2_2 = new Test(2);
            tests.Add(test1); tests.Add(test1_1); tests.Add(test2); tests.Add(test2_2);
            var duplicteTests = tests.GroupBy(x => x.Index).SelectMany(grp => grp.Skip(1));
            Debug.WriteLine(duplicteTests.Count());
        }
        class Test{
            public Test(int index) { Index = index; }
            public int Index;
        }

        private void ToastGroupButton_Click(object sender, RoutedEventArgs e)
        {
            var toastGroup = ToastGroupName.Text;
            var toastScheduled = false;
            ToastNotifier notifier = ToastNotificationManager.CreateToastNotifier();
            var scheduledToasts = notifier.GetScheduledToastNotifications();
            if (scheduledToasts.Any(t => t.Group == toastGroup))
            {
                toastScheduled = true;
            }

            if (toastScheduled) IsToastGroupScheduled.Text = "グループ " + toastGroup +" のスケジュールが存在します";
            else IsToastGroupScheduled.Text = "グループ " + toastGroup + " のスケジュールが存在しません";
        }

        private void CancelToastButton_Click(object sender, RoutedEventArgs e)
        {
            var toastGroup = ToastGroupName.Text;
            var notifier = ToastNotificationManager.CreateToastNotifier();
            IReadOnlyList<ScheduledToastNotification> scheduledToasts = notifier.GetScheduledToastNotifications();

            var toRemove = scheduledToasts.Where(i => i.Group == toastGroup);

            foreach (var item in toRemove)
                notifier.RemoveFromSchedule(item);

            ToastCanceled.Text = toastGroup + " のスケジュールがキャンセルされました";
        }

    }
}