using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

namespace WindowsGoodbye
{
    public static class BackgroundHelper
    {
        public const string BackgroundTaskName = "WindowsGoodbyeAuthTask";
        public static void RegisterIfNeeded()
        {
            var taskRegistered = BackgroundTaskRegistration.AllTasks.Any(task => task.Value.Name == BackgroundTaskName);
            if (taskRegistered) return;
            BackgroundExecutionManager.RemoveAccess();
            var status = BackgroundExecutionManager.RequestAccessAsync().AsTask().GetAwaiter().GetResult();
            if (status == BackgroundAccessStatus.DeniedBySystemPolicy || status == BackgroundAccessStatus.DeniedByUser)
            {
                Debug.WriteLine("Background Execution is denied.");
            }
            else
            {
                var taskBuilder = new BackgroundTaskBuilder
                {
                    Name = BackgroundTaskName,
                    TaskEntryPoint = "WindowsGoodbyeAuthTask.WindowsGoodbyeAuthTask"
                };
                taskBuilder.SetTrigger(new SecondaryAuthenticationFactorAuthenticationTrigger());
                taskBuilder.Register();
            }
        }

        public static void Unregister()
        {
            var taskPair = BackgroundTaskRegistration.AllTasks.FirstOrDefault(t => t.Value.Name == BackgroundTaskName);
            if (default(KeyValuePair<Guid, IBackgroundTaskRegistration>).Equals(taskPair)) return;
            var task = taskPair.Value;
            task.Unregister(false);
        }
    }
}
