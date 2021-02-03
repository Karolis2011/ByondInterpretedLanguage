using ByondLang.ChakraCore.Hosting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ByondLang.UnitTests.ChakraCore
{
    public class CCEngineTests : IClassFixture<CCFixture>
    {
        CCFixture fixture;

        public CCEngineTests(CCFixture cFixture)
        {
            fixture = cFixture;
        }

        [Fact]
        public void ScriptExecution()
        {
            using (new JsContext.Scope(fixture.context))
            {
                var ow = JsContext.RunScript("x = 5");
                var gob = JsValue.GlobalObject;
                var value = gob.GetProperty("x");
                Assert.Equal(ow, value);
                Assert.Equal(JsValueType.Number, value.ValueType);
                Assert.Equal(5, value.ToInt32());
            }
        }

        [Fact]
        public void JsFunctionCall()
        {
            using (new JsContext.Scope(fixture.context))
            {
                var fun = JsContext.RunScript("x = 0; ()=>{x++;}");
                var gob = JsValue.GlobalObject;
                var value = gob.GetProperty("x");
                Assert.Equal(0, value.ToInt32());
                fun.CallFunction(gob);
                value = gob.GetProperty("x");
                Assert.Equal(1, value.ToInt32());
            }
        }

        [Fact]
        public void TMFunction()
        {
            using (new JsContext.Scope(fixture.context))
            {
                var gob = JsValue.GlobalObject;
                var flag = 0;
                Func<int, string> del = (num) =>
                {
                    flag += num;
                    return "Win!";
                };
                gob.SetProperty("fun", fixture.typeMapper.MapToScriptType(del), true);
                var result = JsContext.RunScript("fun(5)");
                var finalResult = result.ToString();
                Assert.Equal(5, flag);
                Assert.Equal("Win!", finalResult);
            }
        }

        [Fact]
        public void TMFunctionOverload()
        {
            using (new JsContext.Scope(fixture.context))
            {
                var gob = JsValue.GlobalObject;
                Func<int, string> del = (num) =>
                {
                    return "Win!";
                };
                Func<string, string> del2 = (str) =>
                {
                    return $"Win! {str}";
                };
                gob.SetProperty("fun", fixture.typeMapper.MapToScriptType(new Delegate[] { del, del2 }), true) ;
                var result = JsContext.RunScript("fun(5)");
                var finalResult = fixture.typeMapper.MapToHostType(result);
                result = JsContext.RunScript("fun('You are winner!')");
                var finalResult2 = fixture.typeMapper.MapToHostType(result);
                Assert.Equal("Win!", finalResult);
                Assert.Equal("Win! You are winner!", finalResult2);
            }
        }

        [Fact]
        public void TMArray()
        {
            using (new JsContext.Scope(fixture.context))
            {
                var gob = JsValue.GlobalObject;
                var array = JsValue.CreateArray(3);
                array.SetIndexedProperty(0, JsValue.FromInt32(1));
                array.SetIndexedProperty(1, JsValue.FromInt32(2));
                array.SetIndexedProperty(2, JsValue.FromInt32(7));
                gob.SetProperty("x", array, true);
                var result = JsContext.RunScript("x[0]");
                var finalResult = fixture.typeMapper.MapToHostType(result);
                Assert.Equal(1, finalResult);
            }
        }
    }
}
