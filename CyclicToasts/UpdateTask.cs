using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

namespace CyclicToasts
{
    public sealed class UpdateTask : IBackgroundTask
    {
        private BackgroundTaskDeferral _deferral;
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            BackgroundExecutionManager.RemoveAccess("App");
            var status = await BackgroundExecutionManager.RequestAccessAsync("App");

            _deferral.Complete();
        }
    }
}
