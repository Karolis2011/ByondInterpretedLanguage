using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ByondLang.Interface
{
    public class JsPFIFOScheduler : JsScheduler, IDisposable
    {
        private object aliveLock = new object();
        private bool alive = true;
        private Thread thread;
        private LinkedList<JsTask> tasks = new LinkedList<JsTask>();
        private AutoResetEvent newTaskEvent = new AutoResetEvent(false);


        public JsPFIFOScheduler()
        {
            thread = new Thread(ExecutionThread);
            thread.Start();
        }

        private JsTask FirstToExecute()
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
            JsTask currentlyExecuting = null;
            while (shouldStayAlive)
            {
                if (currentlyExecuting == null)
                {
                    lock (tasks)
                    {
                        currentlyExecuting = FirstToExecute();
                    }
                }

                if (currentlyExecuting != null)
                {
                    currentlyExecuting.Run();
                    currentlyExecuting = null;
                }
                else
                {
                    lock (aliveLock)
                        shouldStayAlive = alive;
                    if (shouldStayAlive)
                        newTaskEvent.WaitOne();
                }
            }
        }

        public void Dispose()
        {
            lock (aliveLock)
                alive = false;
            newTaskEvent.Reset();
            Thread.SpinWait(1);
            newTaskEvent.Set();
        }
    }
}
