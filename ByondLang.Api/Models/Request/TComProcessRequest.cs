using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ByondLang.Models.Request
{
    public class TComProcessRequest
    {
        public int Id { get; set; }
        public TComSignal Signal { get; set; }
    }
}
