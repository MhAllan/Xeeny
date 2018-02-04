using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Descriptions
{
    enum OperationInvokationType
    {
        OneWay,
        Void,
        Return,

        AwaitableOneWay,
        AwaitableVoid,
        AwaitableReturn
    }
}
