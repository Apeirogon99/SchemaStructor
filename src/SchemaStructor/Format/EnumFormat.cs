using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchemaStructor.Format
{
    internal class EnumFormat
    {
        /// <summary>
        /// {0} 프로젝트 이름
        /// {1} 스키마 이름
        /// {2} eunm context
        /// </summary>
        public static string context =
                        @"

namespace {0}.Models.{1}
{{

        public enum ETableName
        {{
            None,{2}
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
