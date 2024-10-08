﻿using MySqlConnector;
using SchemaStructor.Script;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;

namespace SchemaStructor.Data
{
    public class Schema
    {
        //private ConcurrentDictionary<string, DateTime> tableCache = new ConcurrentDictionary<string, DateTime>();
        private ConcurrentQueue<string> tableNames = new ConcurrentQueue<string>();
        private ConcurrentQueue<Table> tables = new ConcurrentQueue<Table>();

        public Schema()
        {

        }

        public void Export(int workthreadNumber)
        {
            try
            {
                //워크 스레드 개수 설정 ( 1 ~ 자신의 코어 수 )
                {
                    workthreadNumber = Math.Clamp(workthreadNumber, 1, Environment.ProcessorCount);
                }

                using (var connnection = new MySqlConnection(Program.ConnectionString))
                {
                    connnection.Open();

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

                    //Cache 읽어서 수정사항 확인
                    {
                        DirectoryInfo? directoryInfo = Directory.GetParent(Environment.CurrentDirectory);
                        if (directoryInfo != null && directoryInfo.Parent != null)
                        {
                            string cachePath = directoryInfo.Parent.FullName + "\\Cache";
                            if (!Directory.Exists(cachePath))
                            {
                                Directory.CreateDirectory(cachePath);
                            }

                            if (tableNames.Count <= 0)
                            {
                                throw new Exception("데이터베이스의 테이블에 수정사항이 존재하지 않음");
                            }
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
                        DirectoryInfo? directoryInfo = Directory.GetParent(Environment.CurrentDirectory);
                        if (directoryInfo != null && directoryInfo.Parent != null)
                        {
                            string jsonPath = directoryInfo.Parent.FullName + "\\Json";
                            if (!Directory.Exists(jsonPath))
                            {
                                Directory.CreateDirectory(jsonPath);
                            }

                            var orderByTables = tables.OrderBy(table => table.Name).ToList();
                            string jsonString = JsonSerializer.Serialize(orderByTables, new JsonSerializerOptions { WriteIndented = true });

                            File.WriteAllText($"{jsonPath}/{Program.SchemaName}.json", jsonString);
                        }

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
                using (var connnection = new MySqlConnection(Program.ConnectionString))
                {
                    await connnection.OpenAsync();
                    Console.WriteLine("Task : " + tableName);

                    //저장할 테이블 생성
                    Table table = new Table
                    {
                        DbTableName = tableName,
                        Name = ParseTableName(tableName, Program.TableNameSeparator),
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
                                Nullable = (columnsReader.GetString(2) == "YES")
                            };

                            if (column.Type == "enum")
                            {
                                column.Values = ParseEnumValues(columnsReader.GetString(1));
                            }

                            //Default를 따로 지정하지 않았을 경우
                            bool isDefaultNull = columnsReader.IsDBNull(3);
                            if (column.Nullable)
                            {
                                column.Default = "null";
                            }
                            else if (column.Type == "enum")
                            {
                                column.Values = ParseEnumValues(columnsReader.GetString(1));

                                string value = (true == isDefaultNull) ? column.Values[0] : columnsReader.GetString(3);
                                column.Default = ConvertMySqlDefaultToCSharp(value, column.Type);
                            }
                            else
                            {
                                string value = (true == isDefaultNull) ? "" : columnsReader.GetString(3);
                                column.Default = ConvertMySqlDefaultToCSharp(value, column.Type);
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
                var result = typeMapping.FirstOrDefault(word => type.IndexOf(word.Key) != -1);
                return !string.IsNullOrEmpty(result.Key) ? result.Value : "string";
            }
        }

        /// <summary>
        /// 값이 있다면 타입에 맞게 정의
        /// </summary>
        private string ConvertMySqlDefaultToCSharp(string value, string type)
        {
            string convertValue = value.Trim('\'');

            var typeMapping = new Dictionary<string, string>()
                {
                    // 정수 타입 매핑
                    { "byte", convertValue == "" ? "0" : convertValue },
                    { "short", convertValue == "" ? "0" : convertValue },
                    { "int", convertValue == "" ? "0" : convertValue },
                    { "long", convertValue == "" ? "0" : convertValue },

                    // 부동 소수점 타입 매핑
                    { "float", convertValue == "" ? "0" : convertValue },
                    { "double", convertValue == "" ? "0.0" : convertValue },
                    { "decimal", convertValue == "" ? "0" : convertValue },

                    // 문자열 타입 매핑
                    { "string", convertValue == "" ? "string.Empty" : convertValue },

                    // 날짜 및 시간 타입 매핑
                    { "DateTime", "DateTime.MinValue" },
                    { "TimeSpan", "TimeSpan.Zero" },

                    // 기타 타입 매핑
                    { "byte[]", "new byte[]" },
                    { "bool", "false" },
                    { "json", "string.Empty" },
                    { "enum", convertValue }
                };

            // 매핑된 타입 반환, 없는 경우 기본적으로 string 타입 반환
            var result = typeMapping.FirstOrDefault(word => type.IndexOf(word.Key) != -1);
            return !string.IsNullOrEmpty(result.Key) ? result.Value : "string.Empty";
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

            if (string.IsNullOrEmpty(separator))
            {
                return tableName;
            }

            string[] words = tableName.Split(separator);

            for (int i = 0; i < words.Length; i++)
            {
                words[i] = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(words[i]);
            }
        
            return string.Concat(words);
        }

    }
}
