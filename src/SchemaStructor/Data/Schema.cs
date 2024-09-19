using MySqlConnector;
using SchemaStructor.Script;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;

namespace SchemaStructor.Data
{
    public class Schema
    {
        private string connectionString;

        private ConcurrentQueue<string> tableNames = new ConcurrentQueue<string>();
        private ConcurrentQueue<Table> tables = new ConcurrentQueue<Table>();

        public Schema(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public void Export(int workthreadNumber)
        {
            try
            {
                //워크 스레드 개수 설정 ( 1 ~ 자신의 코어 수 )
                {
                    workthreadNumber = Math.Clamp(workthreadNumber, 1, Environment.ProcessorCount);
                }

                using (var connnection = new MySqlConnection(connectionString))
                {
                    connnection.Open();

                    //데이터베이스의 이름 얻기
                    string schemaName = string.Empty;
                    {
                        string getSchemaNameQuery = "SELECT SCHEMA();";
                        MySqlCommand getSchemaNameCommand = new MySqlCommand(getSchemaNameQuery, connnection);

                        using (MySqlDataReader schemaNameReader = getSchemaNameCommand.ExecuteReader())
                        {
                            if (true == schemaNameReader.Read())
                            {
                                schemaName = ParseTableName(schemaNameReader.GetString(0), "_");
                            }
                            else
                            {
                                throw new Exception("데이터베이스의 이름을 얻지 못함");
                            }
                        }
                    }

                    //데이터베이스 모든 테이블 이름 얻기
                    {
                        string getTablesNameQuery = $"SELECT TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA = SCHEMA();";
                        MySqlCommand getTablesNameCommand = new MySqlCommand(getTablesNameQuery, connnection);

                        using (MySqlDataReader tablesNameReader = getTablesNameCommand.ExecuteReader())
                        {
                            while (tablesNameReader.Read())
                            {
                                tableNames.Enqueue(tablesNameReader.GetString(0));
                            }
                        }

                        if (tableNames.Count <= 0)
                        {
                            throw new Exception("데이터베이스의 테이블에 대한 정보가 존재하지 않음");
                        }
                    }

                    //비동기 추출
                    var tasks = new List<Task>();
                    for (int i = 0; i < workthreadNumber; i++)
                    {
                        tasks.Add(DoExportAsync());
                    }

                    Task.WhenAll(tasks).Wait();

                    //Json 직렬화하여 필요시 폴더및 파일 생성
                    {
                        string folderPath = Path.Combine(Program.OutputPath, "Json");
                        if (!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                        }

                        string jsonString = JsonSerializer.Serialize(tables, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText($"{Program.OutputPath}/Json/{schemaName}.json", jsonString);
                    }

                    connnection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error [Schema.Export] : " + ex.Message);
            }
        }

        public async Task DoExportAsync()
        {

            while (tableNames.TryDequeue(out var tableName))
            {
                using (var connnection = new MySqlConnection(connectionString))
                {
                    await connnection.OpenAsync();
                    Console.WriteLine("Task : " + tableName);

                    //저장할 테이블 생성
                    Table table = new Table
                    {
                        Name = ParseTableName(tableName, "_"),
                    };

                    //COLUMN (이름, 타입, NULLABLE, 디폴트) 검색
                    string getColumnsQuery = $@"
                            SELECT COLUMN_NAME, COLUMN_TYPE, IS_NULLABLE, COLUMN_DEFAULT
                            FROM information_schema.columns 
                            WHERE TABLE_NAME = '{tableName}' AND TABLE_SCHEMA = SCHEMA();";


                    //검색한 결과를 Column에 입력
                    using (var getColumnsCommand = new MySqlCommand(getColumnsQuery, connnection))
                    using (MySqlDataReader columnsReader = await getColumnsCommand.ExecuteReaderAsync())
                    {
                        while (await columnsReader.ReadAsync())
                        {
                            Column column = new Column
                            {
                                Name = columnsReader.GetString(0),
                                Type = ConvertMySqlTypeToCSharp(columnsReader.GetString(1)),
                                Nullable = (columnsReader.GetString(2) == "YES"),
                                Default = columnsReader.IsDBNull(3) ? "NULL" : columnsReader.GetString(3),
                            };

                            if (column.Type == "enum")
                            {
                                column.Values = ParseEnumValues(columnsReader.GetString(1));
                            }


                            table.Columns.Add(column);
                        }
                    }

                    tables.Enqueue(table);
                    await connnection.CloseAsync();
                }
            }
        }
        
        /// <summary>
        /// MySQL타입 C#타입과 매핑
        /// </summary>
        private string ConvertMySqlTypeToCSharp(string type)
        {
            type = type.ToLower();

            if(-1 != type.IndexOf("enum"))
            {
                return "enum";
            }
            else
            {
                var typeMapping = new Dictionary<string, string>()
                {
                    // 정수 타입 매핑
                    { "tinyint", "byte" },
                    { "smallint", "short" },
                    { "mediumint", "int" },
                    { "int", "int" },
                    { "bigint", "long" },

                    // 부동 소수점 타입 매핑
                    { "float", "float" },
                    { "double", "double" },
                    { "decimal", "decimal" },

                    // 문자열 타입 매핑
                    { "char", "string" },
                    { "varchar", "string" },
                    { "text", "string" },
                    { "mediumtext", "string" },
                    { "longtext", "string" },

                    // 날짜 및 시간 타입 매핑
                    { "date", "DateTime" },
                    { "datetime", "DateTime" },
                    { "timestamp", "DateTime" },
                    { "time", "TimeSpan" },
                    { "year", "int" },

                    // 기타 타입 매핑
                    { "blob", "byte[]" },
                    { "tinyblob", "byte[]" },
                    { "mediumblob", "byte[]" },
                    { "longblob", "byte[]" },
                    { "bit", "bool" },
                    { "bool", "bool" },
                    { "boolean", "bool" },
                    { "json", "string" },
                };

                // 매핑된 타입 반환, 없는 경우 기본적으로 string 타입 반환
                return typeMapping.ContainsKey(type) ? typeMapping[type] : "string";
            }
        }

        /// <summary>
        /// enum값 추출
        /// </summary>
        private List<string> ParseEnumValues(string value)
        {
            //불필요한 부분 제거
            string cleaned = value.Replace("enum(", "").Replace(")", "").Replace("'", "");

            return cleaned.Split(',').ToList();
        }

        /// <summary>
        /// Split하여 앞글자만 대문자로 변환
        /// </summary>
        private string ParseTableName(string tableName, string separator)
        {
            string[] words = tableName.Split(separator);

            for (int i = 0; i < words.Length; i++)
            {
                words[i] = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(words[i]);
            }
        
            return string.Concat(words);
        }

    }
}
