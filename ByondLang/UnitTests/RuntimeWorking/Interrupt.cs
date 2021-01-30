using ByondLang.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ByondLang.UnitTests.RuntimeWorking
{
    public class Interrupt
    {
        [Fact]
        public async void SimpleWhileTrue()
        {
            using (var runtime = new Runtime(null))
            {
                var program = await runtime.BuildContext((r, c, m) => new BaseProgram(r, c, m));

                var result = program.ExecuteScript("while(true) {}");

                result.Wait(2100);
            }
        }

        [Fact]
        public async void NativeFucntionWhileTrue()
        {
            using (var runtime = new Runtime(null))
            {
                var program = await runtime.BuildContext((r, c, m) => new BaseProgram(r, c, m));

                var result = program.ExecuteScript("while(true) {atob('QQ==')}");

                result.Wait();
            }
        }
    }
}
