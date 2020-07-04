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
        public void ConstructAndDispose()
        {
            using (var runtime = new Runtime())
            {
                var program = runtime.BuildContext((r, c, m) => new BaseProgram(r, c, m));

                var result = program.ExecuteScript("Math.PI * (5+2)");

                result.Wait();
            }
        }
    }
}
