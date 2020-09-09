using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PromDapterDeclarations
{
    [DebuggerDisplay("{Source} {Category} {Name} {Value} {Unit}")]
    public class DataItem
    {
        public Source Source { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }

        public DateTime Timestamp { get; set; }

        public DataValue Value { get; set; }

        public Dictionary<string, DataValue[]> CategoryValues { get; set; }
    }
}
