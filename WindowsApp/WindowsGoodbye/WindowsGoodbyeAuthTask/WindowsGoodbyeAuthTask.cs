using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

namespace WindowsGoodbyeAuthTask
{
    public sealed class WindowsGoodbyeAuthTask : IBackgroundTask
    {
        BackgroundTaskDeferral _deferral;
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            Debug.WriteLine("Background task activated!");
            _deferral = taskInstance.GetDeferral();
        }
    }
}
