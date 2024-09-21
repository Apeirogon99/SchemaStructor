using MySqlConnector;
using System.Collections;
using System.Reflection;

namespace SchemaStructor.Format
{
    public abstract class DbContext
    {
        public DbContext()
        {
            
        }

        protected abstract bool IsValidContext();

        protected async Task<DbTable<T>> LoadDatabaseTable<T>(string connectionString, string query) where T : class, new()
        {
            try
            {
                Dictionary<int, T> datas = new Dictionary<int, T>();

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        using (MySqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            T data = new T();
                            foreach (var prop in properties)
                            {
                                if (reader.GetOrdinal(prop.Name) < 0)
                                {
                                    throw new Exception("테이블이 일치하지 않습니다, 데이터베이스를 업데이트 해주세요");
                                }

                                if (!reader.IsDBNull(reader.GetOrdinal(prop.Name)))
                                {
                                    prop.SetValue(data, reader[prop.Name]);
                                }
                            }
                            datas.Add(reader.GetInt32(0) ,data);
                        }
                    }
                    await conn.CloseAsync();
                }

                return new DbTable<T>(datas);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DbContext.LoadDatabaseTable] : {ex.Message}");
                return new DbTable<T>(null);
            }
        }
    }

    internal class DbContextFormat
    {
        public static string context =
            @"
using System;
using MySqlConnector;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace {0}.Reposiotry
{{
    public abstract class DbContext
    {{
        public DbContext()
        {{
            
        }}

        public abstract bool IsValidContext();

        protected async Task<DbTable<T>> LoadDatabaseTable<T>(string connectionString, string query) where T : class, new()
        {{
            try
            {{
                Dictionary<int, T> datas = new Dictionary<int, T>();

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {{
                    await conn.OpenAsync();
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {{
                        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        using (MySqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {{
                            while (await reader.ReadAsync())
                            {{
                                T data = new T();
                                foreach (var prop in properties)
                                {{
                                    if (reader.GetOrdinal(prop.Name) < 0)
                                    {{
                                        throw new Exception(""테이블이 일치하지 않습니다, 데이터베이스를 업데이트 해주세요"");
                                    }}

                                    if (reader.IsDBNull(reader.GetOrdinal(prop.Name)))
                                    {{
                                        continue;
                                    }}

                                    if(prop.PropertyType.BaseType!.Name == ""Enum"")
                                    {{
                                        prop.SetValue(data, System.Enum.Parse(prop.PropertyType, reader[prop.Name].ToString()!));
                                    }}
                                    else
                                    {{
                                        prop.SetValue(data, reader[prop.Name]);
                                    }}
                                }}
                                datas.Add(reader.GetInt32(0) ,data);
                            }}
                        }}
                    }}
                    await conn.CloseAsync();
                }}

                return new DbTable<T>(datas);
            }}
            catch (Exception ex)
            {{
                Console.WriteLine($""[DbContext.LoadDatabaseTable] : {{ex.Message}}"");
                return new DbTable<T>(null);
            }}
        }}
    }}
}}
            ";
    }
}
