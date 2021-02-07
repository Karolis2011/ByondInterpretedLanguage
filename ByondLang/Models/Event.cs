using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ByondLang.Models
{
    public class Event
    {

        public Event(string eventId)
        {
            EventID = eventId;
        }

        public string EventID { get; private set; }
        virtual public EventType EventType => EventType.Unknown;
        virtual public object EventData => null;
        virtual public bool NeedsToBeResolved => false;

        virtual public void Resolve(object data) { }
        virtual public void Reject(object data) { }

    }
}
