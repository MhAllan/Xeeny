using Microsoft.Extensions.Logging;
using Xeeny.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Api.Base
{
    public abstract class BaseBuilder
    {
        internal protected abstract ISerializer Serializer { get; set; }
        internal protected abstract ILoggerFactory LoggerFactory { get; set; }
    }
}
