using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ByondLang.ChakraCore
{
    public class JsPFIFOScheduler : JsScheduler, IDisposable
    {
        private readonly object aliveLock = new object();
        private bool alive = true;
        private Thread thread;
        private LinkedList<JsTask> tasks = new LinkedList<JsTask>();
        private LinkedList<JsTask> debugTasks = new LinkedList<JsTask>();
        private AutoResetEvent newTaskEvent = new AutoResetEvent(false);
        private AutoResetEvent newDebugTaskEvent = new AutoResetEvent(false);

        private JsTask currentlyExecuting = null;
        private object inBreakLock = new object();
        private bool inBreak = false;

        public JsPFIFOScheduler()
        {
            thread = new Thread(ExecutionThread);
            thread.Start();
        }

        private JsTask FirstToExecute(LinkedList<JsTask> tasks)
        {
            LinkedListNode<JsTask> lowest = tasks.First;
            for(var node = tasks.First; node != null; node = node.Next)
            {
                if (lowest != null && lowest.Value.Priority > node.Value.Priority)
                    lowest = node;
            
            }
            var value = lowest?.Value;
            if(lowest != null)
                tasks.Remove(lowest);
            return value;
        }

        public override void QueueTask(JsTask task)
        {
            newTaskEvent.Reset();
            lock (tasks)
                _ = tasks.AddLast(task);
            newTaskEvent.Set();
        }

        private void ExecutionThread()
        {
            bool shouldStayAlive = true;
            while (shouldStayAlive)
            {
                lock (tasks)
                    currentlyExecuting = FirstToExecute(tasks);

                if (currentlyExecuting != null)
                {
                    currentlyExecuting.Run();
                    currentlyExecuting = null;
                }
                else
                {
                    newTaskEvent.WaitOne();
                }
                lock (aliveLock)
                    shouldStayAlive = alive;
            }
        }

        public override void QueueTaskDebug(JsTask task)
        {
            newDebugTaskEvent.Reset();
            lock (debugTasks)
                _ = debugTasks.AddLast(task);
            newDebugTaskEvent.Set();
        }

        public override void EnterBreakState()
        {
            if (Thread.CurrentThread != thread)
                throw new InvalidOperationException("Current thread is not same as scheduler's thread.");
            bool breakState = true;
            lock (inBreakLock)
                inBreak = true;

            currentlyExecuting.OnBreak();
            JsTask currentlyExecutingDebug = null;
            while (breakState)
            {
                lock (debugTasks)
                    currentlyExecutingDebug = FirstToExecute(debugTasks);

                if (currentlyExecutingDebug != null)
                {
                    currentlyExecutingDebug.Run();
                    currentlyExecutingDebug = null;
                } else
                {
                    newDebugTaskEvent.WaitOne();
                }

                lock (inBreakLock)
                    breakState = inBreak;
            }
            currentlyExecuting.OnResume();
        }

        public override void ExitBreakState()
        {
            newDebugTaskEvent.Reset();
            lock (inBreakLock)
                inBreak = false;
            newDebugTaskEvent.Set();
        }

        public void Dispose()
        {
            lock (aliveLock)
                alive = false;
            newTaskEvent.Reset();
            newDebugTaskEvent.Reset();
            Thread.SpinWait(1);
            newTaskEvent.Set();
            newDebugTaskEvent.Set();
        }
    }
}
