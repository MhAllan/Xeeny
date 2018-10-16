﻿using Xeeny.Api.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Api.Client
{
    public abstract class BaseConnectionBuilder : BaseBuilder
    {
        internal protected abstract ITransportFactory TransportFactory { get; set; }
    }
}
