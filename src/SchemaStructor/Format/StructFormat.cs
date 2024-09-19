
namespace SchemaStructor.Format
{
    internal class StructFormat
    {

        // {0} 테이블 ENUM
        // {1} 테이블 STRUCT 
        public static string context =
            @"
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace {0}.Models.Struct
{{
        {1}
        {2}

}}";

    }

    internal class StructContextFormat
    {
        // {0} 테이블 이름
        // {1} 테이블 컬럼들
        public static string EnumContext =
    @"
    public enum {0}
    {{
        None,{1}
    }}";


        // {0} 테이블 이름
        // {1} 테이블 컬럼들
        public static string StructContext =
    @"  
    public class {0}
    {{{1}
    }}";

        // {0} 컬럼 열거형 이름
        public static string EnumValueContext =
            @"		
        {0},";

        // {0} 컬럼 타입
        // {1} 컬럼 이름
        // {2} 컬럼 디폴트 값
        public static string StructValueContext =
            @"		
        public {0} {1} {{ get; set; }} = {2};";
    }
}
