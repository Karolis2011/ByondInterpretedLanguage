using ByondLang.ChakraCore;
using ByondLang.Interface;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ByondLang.UnitTests.StateTests
{
    public class RuntimeTaskSchedulerTests
    {

        [Fact]
        public void CreateAndDisposeSequential()
        {
            var s = new JsPFIFOScheduler();
            bool flag1 = false;
            bool flag2 = false;
            var task1 = s.Run(() => { flag1 = true; Thread.Sleep(50); });
            var task2 = s.Run(() => flag2 = true);
            task1.Wait(20);
            Assert.True(flag1);
            Assert.False(task1.IsCompleted);
            Assert.False(flag2);
            Assert.False(task2.IsCompleted);
            Assert.True(task2.Wait(500));
            Thread.Sleep(10);
            Assert.True(flag1);
            Assert.True(task1.IsCompleted);
            Assert.True(flag2);
            Assert.True(task2.IsCompleted);
            s.Dispose();
        }

        [Fact]
        public void ProperPriority()
        {
            var s = new JsPFIFOScheduler();
            bool flag1 = false;
            bool flag2 = false;
            bool flag3 = false;
            var task0 = s.Run(() => { Debug.Print("T0."); Thread.Sleep(40); }, priority: JsTaskPriority.INITIALIZATION);
            var task1 = s.Run(() => { Debug.Print("T1."); Thread.Sleep(20); flag1 = true; }, priority: JsTaskPriority.EXECUTION);
            var task2 = s.Run(() => { Debug.Print("T2."); Thread.Sleep(20); flag2 = true; }, priority: JsTaskPriority.LOWEST);
            var task3 = s.Run(() => { Debug.Print("T3."); Thread.Sleep(20); flag3 = true; }, priority: JsTaskPriority.INITIALIZATION);
            Assert.True(task3.Wait(500));
            Assert.True(flag3); // We expect task3 to be finished as we asked
            // We wait for task 1 and then check for flag.
            Assert.True(task1.Wait(500));
            Assert.True(flag1); // we also expect it to be finished
            Assert.False(flag2); // And we expect task2 to not have finished yeat.

        }

        [Fact]
        public void BreakState()
        {
            var s = new JsPFIFOScheduler();
            bool flag1 = false;
            bool flag2 = false;
            bool flag3 = false;
            var task1 = s.Run(() => { s.EnterBreakState(); flag1 = true; });
            var task2 = s.Run(() => { flag3 = true; });
            var task3 = s.RunDebug(() => { flag2 = true; });
            task3.Wait();
            Assert.False(flag1);
            Assert.True(flag2);
            Assert.False(flag3);
            Assert.False(task1.IsCompleted);
            Assert.False(task2.IsCompleted);
            var task4 = s.RunDebug(() => { s.ExitBreakState(); });
            task4.Wait();
            task1.Wait();
            Assert.True(flag1);
            Assert.True(flag2);
            Assert.True(flag3);
        }
    }
}
