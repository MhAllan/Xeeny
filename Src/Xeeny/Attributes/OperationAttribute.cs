using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xeeny.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class OperationAttribute : Attribute
    {
        public string Name { get; set; }
        public bool IsOneWay { get; set; }
    }
}
