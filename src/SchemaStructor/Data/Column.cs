using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchemaStructor.Data
{
    public class Column
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool Nullable { get; set; } = false;
        public string Default { get; set; } = string.Empty;
        public List<string>? Values { get; set; } = null;
    }
}
