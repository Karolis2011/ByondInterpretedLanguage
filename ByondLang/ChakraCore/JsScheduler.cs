﻿using ByondLang.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ByondLang.ChakraCore
{
    public abstract class JsScheduler
    {

        /// <summary>
        /// Enters blocking call and processes tasks scheduled via QueueTaskDebug.
        /// </summary>
        public abstract void EnterBreakState();
        public abstract void ExitBreakState();
        public abstract void QueueTask(JsTask task);
        public abstract void QueueTaskDebug(JsTask task);

        public virtual JsTask<TResult> Run<TResult>(Func<TResult> func, JsTaskPriority priority = JsTaskPriority.LOWEST) =>
            new JsTask<TResult>(func, priority).Start(this);
        public virtual JsTask Run(Action action, JsTaskPriority priority = JsTaskPriority.LOWEST) => 
            new JsTask(action, priority).Start(this);


        public virtual JsTask<TResult> RunTimed<TResult>(Func<TResult> func, Action onTimeout, TimeSpan timeout, JsTaskPriority priority = JsTaskPriority.LOWEST) =>
            new JsTaskTimed<TResult>(func, onTimeout, timeout, priority).Start(this);
        public virtual JsTask RunTimed(Action action, Action onTimeout, TimeSpan timeout, JsTaskPriority priority = JsTaskPriority.LOWEST) => 
            new JsTaskTimed(action, onTimeout, timeout, priority).Start(this);



        public virtual JsTask<TResult> RunDebug<TResult>(Func<TResult> func, JsTaskPriority priority = JsTaskPriority.LOWEST) =>
            new JsTask<TResult>(func, priority).StartDebug(this);
        public virtual JsTask RunDebug(Action action, JsTaskPriority priority = JsTaskPriority.LOWEST) => 
            new JsTask(action, priority).StartDebug(this);

        public virtual JsTask<TResult> RunDebugTimed<TResult>(Func<TResult> func, Action onTimeout, TimeSpan timeout, JsTaskPriority priority = JsTaskPriority.LOWEST) =>
            new JsTaskTimed<TResult>(func, onTimeout, timeout, priority).StartDebug(this);
        public virtual JsTask RunDebugTimed(Action action, Action onTimeout, TimeSpan timeout, JsTaskPriority priority = JsTaskPriority.LOWEST) => 
            new JsTaskTimed(action, onTimeout, timeout, priority).StartDebug(this);
    }
}
