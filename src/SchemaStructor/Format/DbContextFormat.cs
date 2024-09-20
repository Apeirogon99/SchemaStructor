using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net.Http.Headers;

namespace SchemaStructor.Format
{
    public class DbTable<TValue> : IEnumerable<KeyValuePair<int, TValue>> where TValue : class
    {
        private readonly ImmutableDictionary<int, TValue> _data;

        public DbTable(ImmutableDictionary<int, TValue> data)
        {
            this._data = data;
        }

        public TValue Find(int key)
        {
            return this._data[key];
        }

        public IEnumerator<KeyValuePair<int, TValue>> GetEnumerator()
        {
            return this._data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

    }


    public class DbContext
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

    internal class DbContextFormat
    {
        public static string context =
            @"
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net.Http.Headers;

namespace {0}.Reposiotry
{{
    public class DbContext
    {{

    }}
}}
            ";
    }
}
