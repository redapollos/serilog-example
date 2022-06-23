using Microsoft.AspNetCore.Http;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;
using serilog_example;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
 * This component provides the ability to set the LogEventId in the database
 * using Serilog.  
 * 
 * When a log is called using the MS ILogger interface, a type is sometimes given first and that will
 * map to the EventId property, which we can match up against the LogEvents enum. * 
 */

namespace RainstormTech.Core.Components.Logging
{
    public class CommonEventEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            // check for a http request
            if (logEvent.MessageTemplate.Text.StartsWith("HTTP"))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("LogEventId", (ushort)LogEvents.HttpRequest));
                return;
            }

            // try to find the EventId, which is the first param passed in when using log.logInformation ...etc
            if (!logEvent.Properties.TryGetValue("EventId", out var propertyValue) ||
               !(propertyValue is StructureValue structureValue))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("LogEventId", (ushort)LogEvents.Unknown));
                return;
            }

            // find the Id sub-property
            for (var i = 0; i < structureValue.Properties.Count; ++i)
            {
                if (!"Id".Equals(structureValue.Properties[i].Name, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!(structureValue.Properties[i].Value is ScalarValue scalarValue))
                {
                    continue;
                }

                var eventId = (ushort)(int)scalarValue.Value;
                logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("LogEventId", eventId));
                return;
            }

            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("LogEventId", (ushort)LogEvents.Unknown));
        }
    }
}
