using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Xeeny.Api.Base;

public static class BaseBuilderLoggersExtensions
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
