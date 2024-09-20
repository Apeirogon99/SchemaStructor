using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchemaStructor.Format
{
    internal class InterfaceFormat
    {
        /// <summary>
        /// {0} Project name
        /// {1} Schema name
        /// {2} Tables
        /// </summary>
        public static string context =
            @"using {0}.Models.{1};

namespace {0}.Reposiotry.Interfaces
{{

    public interface I{1}Context : IDisposable
    {{{2}
    }}

}}";
    }

    internal class InterfaceContextFormat
    {
        /// <summary>
        /// {0} Table structor name
        /// {1} Table name
        /// </summary>
        public static string TablesContext = 
            @"
            public DbTable<{0}> {1} {{ get; set; }}";
    }
}
