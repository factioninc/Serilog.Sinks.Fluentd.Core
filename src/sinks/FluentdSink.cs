using System;
using System.Net;
using Serilog.Core;
using Serilog.Events;
using Serilog.fluentd;
using Serilog.Formatting.Display;
using static Serilog.fluentd.FluentdHandler;

namespace Serilog.Sinks.SystemConsole
{
    class FluentdSink : ILogEventSink
    {
        private readonly FluentdHandler handler;
        public FluentdSink()
        {
            handler = FluentdHandler.CreateHandler("test", new FluentdHandlerSettings()
            {
                Host = IPAddress.Parse("127.0.0.1"),
                Port = 24224,
                MaxBuffer = 1024*1024*10
            }).Result;
        }
        
        public void Emit(LogEvent logEvent)
        {
            var outputProperties = OutputProperties.GetOutputProperties(logEvent);

            Console.WriteLine(logEvent.ToString());
            handler.Emit("test", "testing testing").Wait();
        }
    }
}