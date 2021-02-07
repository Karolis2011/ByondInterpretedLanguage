using ByondLang.ChakraCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ByondLang.UnitTests.ChakraCore
{
    public class TMTests : IClassFixture<CCFixture>
    {

        CCFixture fixture;

        public TMTests(CCFixture cFixture)
        {
            fixture = cFixture;
        }

        [Fact]
        public void BasicTypeTest()
        {
            using var tm = new ByondLang.ChakraCore.TypeMapper();
            using (new JsContext.Scope(fixture.context))
            {
                var glob = JsValue.GlobalObject;
                glob.SetProperty("AT", tm.MTS((Action<bool>)Assert.True), true);
                glob.SetProperty("AF", tm.MTS((Action<bool>)Assert.False), true);
                glob.SetProperty("tv_string1", tm.MTS("Test"), true);
                glob.SetProperty("tv_string2", tm.MTS("Uhm"), true);
                glob.SetProperty("tv_number1", tm.MTS(10), true);
                glob.SetProperty("tv_number2", tm.MTS(1.5512), true);
                var TestVal = new TMTestClass();
                glob.SetProperty("tv_obj", tm.MTS(TestVal), true);

                JsContext.RunScript(@"
AT(tv_string1 == 'Test')
AT(tv_string2 == 'Uhm')
AT(tv_number1 == 10)
AT(tv_number2 == 1.5512)

AT(tv_obj.Flag == 0)
AT(tv_obj.TryTrue())
AF(tv_obj.false())
");

                TestVal.Flag += 1;
                JsContext.RunScript(@"
AT(tv_obj.Flag == 1)
tv_obj.Flag = 12
");
                Assert.Equal(12, TestVal.Flag);
            }
        }
    }
}
