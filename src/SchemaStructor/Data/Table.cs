using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchemaStructor.Data
{
    public class Table
    {
        public string Name { get; set; } = string.Empty;
        public string DbTableName { get; set; } = string.Empty;
        public List<Column> Columns { get; set; } = new List<Column>();
    }
}
