using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchemaStructor.Format
{
    internal interface IDbRecord
    {
        public string PrimaryKey { get; set; }
    }

    internal class DbRecordFormat
    {
        /// <summary>
        /// {0} Project name
        /// {1} Schema name
        /// {2} Tables
        /// </summary>
        public static string context =
            @"
using System;
using {0}.Models.{1};

namespace {0}.Reposiotry
{{

    public interface IDbRecord
    {{
        public string PrimaryKey {{ get; set; }}
    }}

}}";
    }
}
