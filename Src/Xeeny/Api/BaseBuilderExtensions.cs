using Microsoft.Extensions.Logging;
using Xeeny.Api.Base;
using Xeeny.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny
{
    public static class BaseBuilderExtensions
    {
        public static TBuilder WithSerializer<TBuilder>(this TBuilder builder, ISerializer serializer)
            where TBuilder : BaseBuilder
        {
            builder.Serializer = serializer;
            return builder;
        }

        public static TBuilder WithMessagePackSerializer<TBuilder>(this TBuilder builder)
            where TBuilder : BaseBuilder
        {
            builder.Serializer = new MessagePackSerializer();
            return builder;
        }

        public static TBuilder WithLoggerFactory<TBuilder>(this TBuilder builder, ILoggerFactory loggerFactory)
            where TBuilder : BaseBuilder
        {
            builder.LoggerFactory = loggerFactory;
            return builder;
        }
    }
}
