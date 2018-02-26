using Xeeny.Api.Base;

using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json;

namespace Xeeny
{
    public static class BaseBuilderExtensions
    {
        public static TBuilder WithJsonSerializer<TBuilder>(this TBuilder builder)
            where TBuilder : BaseBuilder
        {
            builder.Serializer = new Serialization.JsonSerializer();
            return builder;
        }

        public static TBuilder WithJsonSerializer<TBuilder>(this TBuilder builder, JsonSerializerSettings settings)
            where TBuilder : BaseBuilder
        {
            builder.Serializer = new Serialization.JsonSerializer(settings);
            return builder;
        }
    }
}
