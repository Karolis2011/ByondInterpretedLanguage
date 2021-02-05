using ByondLang.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ByondLang.ChakraCore
{
    public abstract class JsScheduler
    {
        protected BaseProgram program;
        public JsScheduler(BaseProgram program)
        {
            this.program = program;
        }

        public abstract void EnterBreakState();
        public abstract void QueueTask(JsTask task);
        public abstract void QueueTaskDebug(JsTask task);

        public virtual JsTask<TResult> Run<TResult>(Func<TResult> func, JsTaskPriority priority = JsTaskPriority.LOWEST) => 
            new JsTask<TResult>(func, priority).Start(this);
        public virtual JsTask Run(Action action, JsTaskPriority priority = JsTaskPriority.LOWEST) => new JsTask(action, priority).Start(this);

        public virtual JsTask<TResult> RunDebug<TResult>(Func<TResult> func, JsTaskPriority priority = JsTaskPriority.LOWEST) =>
            new JsTask<TResult>(func, priority).StartDebug(this);
        public virtual JsTask RunDebug(Action action, JsTaskPriority priority = JsTaskPriority.LOWEST) => new JsTask(action, priority).StartDebug(this);
    }
}
