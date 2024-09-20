using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net.Http.Headers;

namespace SchemaStructor.Format
{
    public class SchemaContext
    {
        //public DbTable<string> reward { get; }
        //public string str;

        //public DbContext()
        //{
        //    reward = new DbTable<string>(LoadTable<string>());
        //}

        //private ImmutableDictionary<int, TValue> LoadTable<TValue>()
        //{

        //}

    }

    internal class SchemaContextFormat
    {
        public static string context =
            @"
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net.Http.Headers;
using {0}.Reposiotry;
using {0}.Reposiotry.Interfaces;

namespace {0}.Reposiotry.{1}
{{
    public class {1}Context : DbContext, I{1}Context
    {{

    }}
}}
            ";
    }
}
