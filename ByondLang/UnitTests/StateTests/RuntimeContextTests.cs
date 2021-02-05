using ByondLang.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ByondLang.UnitTests.StateTests
{
    public class RuntimeContextTests
    {
        [Fact]
        public async void ConstructAndDispose()
        {
            using (var program = new BaseProgram(null))
            {
                await program.InitializeState();
                var result = program.ExecuteScript("Math.PI * (5+2)");

                Assert.True(result.Wait(200));
            }
        }
    }
}
