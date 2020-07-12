using System;
using System.Diagnostics;

namespace PromDapterDeclarations
{
    [DebuggerDisplay("{Object}")]
    public class DataValue
    {
        public Type Type { get; set; }
        public object Object { get; set; }
    }
}