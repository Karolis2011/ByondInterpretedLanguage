using ByondLang.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ByondLang.ChakraCore
{
    public enum JsTaskPriority
    {
        INITIALIZATION,
        EXECUTION,
        CALLBACK,
        PROMISE,
        LOWEST,
    }

    public enum JsTaskState
    {
        Initialized,
        Pending,
        Running,
        Complete,
        Failed
    }

    public class JsTask
    {
        public bool IsCompleted => State == JsTaskState.Failed || State == JsTaskState.Complete;
        public JsTaskPriority Priority { get; protected set; }
        public JsTaskState State { get; protected set; } = JsTaskState.Initialized;

        private Action m_action;
        protected LinkedList<Action> finishedCallbacks = new LinkedList<Action>();

        protected JsTask() { }

        public JsTask(Action function, JsTaskPriority priority = JsTaskPriority.LOWEST)
        {
            m_action = function;
            Priority = priority;
        }

        public virtual void Run()
        {
            if (IsCompleted)
            {
                throw new Exception("Task is already complete.");
            }
            State = JsTaskState.Running;
            try
            {
                m_action();
            }
            catch (Exception)
            {
                State = JsTaskState.Failed;
                throw;
            }
            finally
            {
                State = JsTaskState.Complete;
            }
            foreach (Action callback in finishedCallbacks)
            {
                callback();
            }
            finishedCallbacks.Clear();
        }

        public JsTask Start(JsScheduler scheduler)
        {
            scheduler.QueueTask(this);
            return this; 
        }

        public JsTask StartDebug(JsScheduler scheduler)
        {
            scheduler.QueueTaskDebug(this);
            return this;
        }

        public JsTaskAwaiter GetAwaiter()
        {
            return new JsTaskAwaiter(this);
        }

        public void RegisterOnCompleteCallback(Action action)
        {
            finishedCallbacks.AddLast(action);
        }

        public virtual void OnBreak() { }
        public virtual void OnResume() { }

        public bool Wait(TimeSpan timeout) => SpinWait.SpinUntil(() => IsCompleted, timeout);
        public void Wait() => SpinWait.SpinUntil(() => IsCompleted);

        public bool Wait(int timeout) => Wait(new TimeSpan(TimeSpan.TicksPerMillisecond * timeout));
    }

    public class JsTask<TResult>: JsTask
    {
        Delegate m_action;
        TResult result = default;
        
        public JsTask(Func<TResult> function, JsTaskPriority priority = JsTaskPriority.LOWEST)
        {
            m_action = function;
            Priority = priority;
        }

        public override void Run()
        {
            if (IsCompleted)
            {
                throw new Exception("Task is already complete.");
            }
            State = JsTaskState.Running;
            try
            {
                if (m_action is Action action)
                {
                    action();
                }
                else if (m_action is Func<TResult> func)
                {
                    result = func();
                }
                else
                {
                    throw new ArgumentException("Action type is not supported.");
                }
            }
            catch (Exception)
            {
                State = JsTaskState.Failed;
                throw;
            }
            finally
            {
                State = JsTaskState.Complete;
            }
            foreach (Action callback in finishedCallbacks)
            {
                callback();
            }
            finishedCallbacks.Clear();
        }

        public new JsTask<TResult> Start(JsScheduler scheduler)
        {
            scheduler.QueueTask(this);
            return this;
        }

        public new JsTask<TResult> StartDebug(JsScheduler scheduler)
        {
            scheduler.QueueTaskDebug(this);
            return this;
        }

        public new JsTaskAwaiter<TResult> GetAwaiter()
        {
            return new JsTaskAwaiter<TResult>(this);
        }

        public TResult GetResult()
        {
            return result;
        }

        
    }

    public class JsTaskAwaiter : INotifyCompletion
    {
        protected JsTask _task;

        public bool IsCompleted => _task.IsCompleted;

        protected JsTaskAwaiter() { }
        public JsTaskAwaiter(JsTask task)
        {
            _task = task;
        }

        public void OnCompleted(Action continuation) => _task.RegisterOnCompleteCallback(continuation);

        public object GetResult() => null;
    }

    public class JsTaskAwaiter<TResult> : JsTaskAwaiter
    {
        protected JsTask<TResult> _ttask;


        public JsTaskAwaiter(JsTask<TResult> task)
        {
            _task = task;
            _ttask = task;
        }

        public new TResult GetResult() => _ttask.GetResult();
    }
}
