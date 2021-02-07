using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ByondLang.Models
{
    public class EventDebugBreak : Event
    {
        public EventDebugBreak(string eventId, ) : base(eventId)
        {

        }

        public override EventType EventType => EventType.DebugBreak;

        public override object EventData => new { };

    }
}
