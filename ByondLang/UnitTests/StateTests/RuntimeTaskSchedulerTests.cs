using ByondLang.Interface;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ByondLang.UnitTests.StateTests
{
    public class RuntimeTaskSchedulerTests
    {
        [Fact]
        public void CreateAndDisposeExecutionStop()
        {
            var s = new RuntimeTaskScheduler();
            var factory = new TaskFactory(s);
            s.Dispose();
            bool flag = false;
            var task = factory.StartNew(() => flag = true);
            task.Wait(20);
            Assert.False(flag);
            Assert.False(task.IsCompleted);
        }

        [Fact]
        public void CreateAndDisposeSequential()
        {
            var s = new RuntimeTaskScheduler();
            var factory = new TaskFactory(s);
            bool flag1 = false;
            bool flag2 = false;
            var task1 = factory.StartNew(() => { flag1 = true; Thread.Sleep(50); });
            var task2 = factory.StartNew(() => flag2 = true);
            Thread.Sleep(20);
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
            var s = new RuntimeTaskScheduler();
            var factory = new TaskFactory(s);
            bool flag1 = false;
            bool flag2 = false;
            bool flag3 = false;
            var task1 = factory.StartNew(() => { Thread.Sleep(20); flag1 = true; });
            var task2 = factory.StartNew(() => { Thread.Sleep(20); flag2 = true; });
            var task3 = factory.StartNew(() => { Thread.Sleep(20); flag3 = true; });
            s.PrioritizeTask(task3);
            Assert.True(task3.Wait(500));
            Assert.True(flag3); // We expect task3 to be finished as we asked
            // We wait for task 1 and then check for flag.
            Assert.True(task1.Wait(500));
            Assert.True(flag1); // we also expect it to be finished
            Assert.False(flag2); // And we expect task2 to not have finished yeat.

        }
    }
}
