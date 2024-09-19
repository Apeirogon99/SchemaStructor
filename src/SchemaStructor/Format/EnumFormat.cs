using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchemaStructor.Format
{
    internal class EnumFormat
    {
        // {0} 테이블 이름
        public static string context =
                        @"

namespace {0}.Models.Enum
{{

        public enum ETableName
        {{
            None,{1}
        }}

}}";

    }

    internal class EnumContextFormat
    {
        // {0} 테이블 이름
        public static string EnumContext =
            @"
            {0},";
    }

}
