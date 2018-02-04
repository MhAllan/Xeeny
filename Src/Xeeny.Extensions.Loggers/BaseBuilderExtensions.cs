using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;
using Xeeny.Api.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny
{
    public static class BaseBuilderExtensions
    {
        public static TBuilder WithConsoleLogger<TBuilder>(this TBuilder builder, LogLevel logLevel = LogLevel.None)
            where TBuilder : BaseBuilder
        {
            builder.LoggerFactory.AddConsole(logLevel);
            return builder;
        }

        public static TBuilder WithDebugLogger<TBuilder>(this TBuilder builder, LogLevel logLevel = LogLevel.None)
            where TBuilder : BaseBuilder
        {
            builder.LoggerFactory.AddDebug(logLevel);
            return builder;
        }
    }
}
