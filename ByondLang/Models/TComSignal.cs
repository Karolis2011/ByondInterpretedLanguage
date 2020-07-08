using ByondLang.ChakraCore.Hosting;
using ByondLang.ChakraCore.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ByondLang.Models
{
    [JsObject]
    public class TComSignal
    {
        [JsMapped]
        public string Content { get; set; } = "*beep*";
        [JsMapped]
        public int Freq { get; set; } = 1459;
        [JsMapped]
        public string Source { get; set; } = "Telecomms Broadcaster";
        [JsMapped]
        public string Job { get; set; } = "Machine";
        [JsMapped]
        public bool Pass { get; set; } = true;
        [JsMapped]
        public string Verb { get; set; } = "says";
        [JsMapped]
        public string Language { get; set; } = "Ceti Basic";
        public string Reference { get; set; }

        [JsCallable]
        public TComSignal clone()
        {
            var n = new TComSignal();
            n.Content = Content;
            n.Freq = Freq;
            n.Source = Source;
            n.Job = Job;
            n.Verb = Verb;
            n.Language = Language;
            return n;
        }
    }
        
    
}
