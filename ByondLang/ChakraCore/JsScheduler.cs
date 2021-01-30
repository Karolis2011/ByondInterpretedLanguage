using ByondLang.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ByondLang.ChakraCore
{
    public abstract class JsScheduler
    {

        public abstract void QueueTask(JsTask task);

        public virtual JsTask<TResult> Run<TResult>(Func<TResult> func, BaseProgram program = null, JsTaskPriority priority = JsTaskPriority.LOWEST) => 
            new JsTask<TResult>(func, priority, program).Start(this);
        public virtual JsTask Run(Action action, BaseProgram program = null, JsTaskPriority priority = JsTaskPriority.LOWEST) => new JsTask(action, priority, program).Start(this);
    }
}
