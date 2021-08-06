using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StreamlineUtilities
{
    // .net Framework 4.0 compatible

    // Wrapper class for TaskFactory wipped together to sort of simulate BackgroundWorker class but use the better Tasks technology.
    // Just mostly trying this out.
    public class TaskWorkerProgress
    {
        public int ProgressPercentage { get; set; }
        public object Progress { get; set; }
    }

    public class TaskWorkerEventArgs
    {
        public object Parameters { get; set; }
        public int ProgressPercentage { get; set; }
        public object Progress { get; set; }
        public object Results { get; set; }
        public bool Cancelled { get; set; } = false;
        public bool Error { get; set; } = false;
    }

    public class TaskWorker : IDisposable
    {
        private Task task = null;
        private CancellationTokenSource source = null;
        private CancellationToken token = CancellationToken.None;
        private TaskWorkerEventArgs taskWorkerEventArgs = new TaskWorkerEventArgs();
        private TaskScheduler taskScheduler = TaskScheduler.FromCurrentSynchronizationContext();

        public Action<object, TaskWorkerEventArgs> TaskAction { get; set; }
        public Action<object, TaskWorkerProgress> TaskProgress { get; set; }
        public Action<object, TaskWorkerEventArgs> TaskComplete { get; set; }

        private TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
        private readonly TimeSpan defaultTaskTimeout = TimeSpan.FromSeconds(0);
        public TimeSpan TaskTimeout { get; set; } = TimeSpan.FromSeconds(0);

        private void setTaskTimeout(TimeSpan taskTimeout)
        {
            System.Timers.Timer timeoutTimer = new System.Timers.Timer();
            timeoutTimer.Elapsed += (obj, args) =>
            {
                taskCompletionSource.TrySetResult(true);
            };
            timeoutTimer.Interval = taskTimeout.TotalMilliseconds;
            timeoutTimer.AutoReset = false;
            timeoutTimer.Start();

            try
            {
                taskCompletionSource.Task.ContinueWith((t) => CancelTask(), taskScheduler);
            }
            catch (Exception Ex)
            {
                LogUtilities.Exception("TaskWorker Error: Failed to run timeout task.", Ex);
            }
        }

        private bool _CancellationRequested = false;
        public bool CancellationRequested
        {
            get
            {
                return _CancellationRequested;
            }
        }

        private bool _IsBusy = false;
        public bool IsBusy
        {
            get
            {
                return _IsBusy;
            }
        }

        public void StartTask()
        {
            runTask(null, TaskTimeout);
        }

        public void StartTask(object paramaters)
        {
            runTask(paramaters, TaskTimeout);
        }

        public void StartTask(object paramaters, TimeSpan taskTimeout)
        {
            runTask(paramaters, taskTimeout);
        }

        private void runTask(object paramaters, TimeSpan taskTimeout)
        {
            if (_IsBusy || TaskAction == null)
            {
                // This should probrably throw or something else.
                return;
            }

            Dispose(); // If we are reusing an instance of this class, we want to clean it up first.

            if (TimeSpan.Compare(taskTimeout, defaultTaskTimeout) != 0)
            {
                setTaskTimeout(taskTimeout);
            }

            source = new CancellationTokenSource();
            token = source.Token;
            taskWorkerEventArgs.Parameters = paramaters;
            _IsBusy = true;
            try
            {
                task = Task.Factory.
                    StartNew(() => TaskAction(this, taskWorkerEventArgs), token, TaskCreationOptions.None, TaskScheduler.Default).
                    ContinueWith((t) => taskComplete(), taskScheduler);
            }
            catch (Exception Ex)
            {
                LogUtilities.Exception("TaskWorker Error: Failed to run worker task.", Ex);
            }
        }

        public void CancelTask()
        {
            if (source != null)
            {
                try
                {
                    source.Cancel();
                }
                finally
                {
                    // Nothing really needs to be here.
                }
            }

            if (taskCompletionSource.Task != null && taskCompletionSource.Task.IsCompleted)
            {
                taskCompletionSource.Task.Dispose();
                taskCompletionSource = null;
            }

            _CancellationRequested = true;
            taskWorkerEventArgs.Cancelled = true;
            taskComplete();
        }

        public void ReportProgressWait(object progress, int progressPercentage)
        {
            reportAsync(true, new TaskWorkerProgress()
            {
                Progress = progress,
                ProgressPercentage = progressPercentage,
            });
        }

        public void ReportProgressWait()
        {
            reportAsync(true, new TaskWorkerProgress());
        }

        public void ReportProgressAsync(object progress, int progressPercentage)
        {
            reportAsync(false, new TaskWorkerProgress()
            {
                Progress = progress,
                ProgressPercentage = progressPercentage,
            });
        }

        public void ReportProgressAsync()
        {
            reportAsync(false, new TaskWorkerProgress());
        }

        private void reportAsync(bool wait, TaskWorkerProgress taskWorkerProgress)
        {
            if (TaskProgress == null)
            {
                return;
            }

            Task reportTask = null;
            try
            {
                reportTask = Task.Factory.StartNew(() => TaskProgress(this, taskWorkerProgress), CancellationToken.None, TaskCreationOptions.None, taskScheduler);
                if (wait)
                {
                    reportTask.Wait();
                }
            }
            catch (Exception Ex)
            {
                LogUtilities.Exception("TaskWorker Error: Failed to run timeout task.", Ex);
            }
            finally
            {
                if (reportTask != null)
                {
                    reportTask.Dispose();
                    reportTask = null;
                }
            }
        }

        // Completed event that runs on the initial task start calling thread.
        public void taskComplete()
        {
            // If the IsBusy is false, we just want to fizzle out. This means the Task was cancelled already.
            if (IsBusy == false)
            {
                return;
            }
            _IsBusy = false;

            if (TaskComplete != null)
            {
                TaskComplete(this, taskWorkerEventArgs);
            }

            Dispose();
        }

        // Although this class method internally cleans up it is still good to expose this method just in case.
        public void Dispose()
        {
            taskWorkerEventArgs = new TaskWorkerEventArgs();

            try
            {
                if (task != null && task.IsCompleted)
                {
                    task.Dispose();
                    task = null;
                }

                if (source != null)
                {
                    source.Dispose();
                    source = null;
                }
            }
            catch (Exception Ex)
            {
                // The above may fail is the task is somehow dead locked. Will look into this more.
                LogUtilities.Exception("TaskWorker Error: Failed to dispose TaskWorker resources.", Ex);
            }
        }
    }
}
