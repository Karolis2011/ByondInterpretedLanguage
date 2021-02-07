using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ByondLang.ChakraCore
{
    public class JsTaskTimed : JsTask
    {
        protected Action timeoutAction;
        protected Stopwatch timer = new Stopwatch();
        protected TimeSpan timeout;


        public JsTaskTimed(Action function, Action onTimeout, TimeSpan timeout, JsTaskPriority priority = JsTaskPriority.LOWEST) : base(function, priority)
        {
            timeoutAction = onTimeout;
            this.timeout = timeout;
        }

        public override void Run()
        {
            timer.Start();
            using (var timer = new Timer(TimmerCheck, null, TimeSpan.Zero, new TimeSpan(10_000)))
            {
                base.Run();
            }
            timer.Stop();
        }

        private void TimmerCheck(object state)
        {
            if(timer.Elapsed > timeout)
            {
                timeoutAction();
                timer.Stop();
            }
        }

        public override void OnBreak()
        {
            base.OnBreak();
            timer.Stop();
        }

        public override void OnResume()
        {
            base.OnResume();
            timer.Start();
        }
    }

    public class JsTaskTimed<TResult> : JsTask<TResult>
    {
        protected Action timeoutAction;
        protected Stopwatch timer = new Stopwatch();
        protected TimeSpan timeout;


        public JsTaskTimed(Func<TResult> function, Action onTimeout, TimeSpan timeout, JsTaskPriority priority = JsTaskPriority.LOWEST) : base(function, priority)
        {
            timeoutAction = onTimeout;
            this.timeout = timeout;
        }

        public override void Run()
        {
            timer.Start();
            using (var timer = new Timer(TimmerCheck, null, TimeSpan.Zero, new TimeSpan(10_000)))
            {
                base.Run();
            }
            timer.Stop();
        }

        private void TimmerCheck(object state)
        {
            if (timer.Elapsed > timeout)
            {
                timeoutAction();
                timer.Stop();
            }
        }

        public override void OnBreak()
        {
            base.OnBreak();
            timer.Stop();
        }

        public override void OnResume()
        {
            base.OnResume();
            timer.Start();
        }
    }
}
