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
            task2.Wait();
            Thread.Sleep(10);
            Assert.True(flag1);
            Assert.True(task1.IsCompleted);
            Assert.True(flag2);
            Assert.True(task2.IsCompleted);
        }
    }
}
