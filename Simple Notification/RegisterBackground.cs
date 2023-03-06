using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

namespace Simple_Notification
{
    public static class RegisterBackground
    {
        private const string _backgroundTaskName = "CyclicToasts";
        private const string _backgroundTaskEntryPoint = "CyclicToasts.MyBackgroundTask";
        public static bool IsBackgroundRegistered()
        {
            var taskRegistered = false;
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == _backgroundTaskName)
                {
                    taskRegistered = true;
                    break;
                }
            }
            return taskRegistered;
        }
        public static void Register()
        {
            var taskRegistered = false;
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == _backgroundTaskName)
                {
                    taskRegistered = true;
                    break;
                }
            }
            if (taskRegistered)
                return;

            var builder = new BackgroundTaskBuilder();
            builder.Name = _backgroundTaskName;
            builder.TaskEntryPoint = _backgroundTaskEntryPoint;
            builder.SetTrigger(new TimeTrigger(freshnessTime: 15, oneShot: false));
            _ = builder.Register();
        }

        public static bool IsBackgroundTaskNeeded()
        {
            var toasts = App.GetMyToastContents();
            return !(toasts.Any(x => x.IsEnabled && x.Options.RType != RepeatType.once));
        }
    }
}
