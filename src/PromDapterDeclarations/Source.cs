using System.Diagnostics;

namespace PromDapterDeclarations
{
    [DebuggerDisplay("{SourceName}")]
    public class Source
    {
        public string SourceName { get; set; }
    }
}