using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchemaStructor.Data
{
    public class History
    {
        public string table_name {  get; set; } = string.Empty;
        public DateTime create_time { get; set; } = DateTime.MinValue;
        public DateTime update_time { get; set; } = DateTime.MinValue;
    }
}
