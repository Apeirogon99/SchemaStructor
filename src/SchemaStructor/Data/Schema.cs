using MySqlConnector;
using SchemaStructor.Script;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Xml.Linq;

namespace SchemaStructor.Data
{
    public class Schema
    {
        //private ConcurrentDictionary<string, DateTime> tableCache = new ConcurrentDictionary<string, DateTime>();
        private ConcurrentDictionary<string, History> histories = new ConcurrentDictionary<string, History>();
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
                        string getTablesHistoryQuery = $"" +
                            $"SELECT TABLE_NAME, CREATE_TIME, UPDATE_TIME " +
                            $"FROM INFORMATION_SCHEMA.TABLES " +
                            $"WHERE TABLE_SCHEMA = SCHEMA();";
                        MySqlCommand getTablesHistoryCommand = new MySqlCommand(getTablesHistoryQuery, connnection);

                        using (MySqlDataReader tablesHistoryReader = getTablesHistoryCommand.ExecuteReader())
                        {

                            while (tablesHistoryReader.Read())
                            {
                                History history = new History();
                                history.table_name = tablesHistoryReader.GetString(0);
                                history.create_time = tablesHistoryReader.IsDBNull(1) ? DateTime.MinValue : tablesHistoryReader.GetDateTime(1);
                                history.update_time = tablesHistoryReader.IsDBNull(2) ? DateTime.MinValue : tablesHistoryReader.GetDateTime(2);
                                histories.TryAdd(history.table_name, history);
                            }
                        }

                        if (histories.Count <= 0)
                        {
                            throw new Exception("데이터베이스의 테이블에 대한 정보가 존재하지 않음");
                        }
                    }

                    //Cache 읽어서 수정사항 확인
                    {
                        DirectoryInfo? directoryInfo = Directory.GetParent(Environment.CurrentDirectory);
                        if (directoryInfo != null && directoryInfo.Parent != null)
                        {
                            string cachePath = directoryInfo.Parent.Parent.Parent.FullName + "\\History";
                            if (!Directory.Exists(cachePath))
                            {
                                Directory.CreateDirectory(cachePath);
                            }

                            // 폴더에 똑같은 테이블과 비교하여 update time은 변경되었는지 확인
                            List<History> newHistories = histories.Values.OrderBy(history => history.table_name).ToList();
                            string[] cahceJsonFilePaths = Directory.GetFiles(cachePath, $"*.json");
                            string? schemaFilePath = cahceJsonFilePaths.FirstOrDefault(file => file.Contains($"{Program.SchemaName}.json"));
                            if (schemaFilePath != null)
                            {
                                string jsonContent = File.ReadAllText(schemaFilePath);
                                var oldHistories = JsonSerializer.Deserialize<List<History>>(jsonContent);
                                if (oldHistories != null && oldHistories.Count != 0)
                                {
                                    foreach(var oldHistory in oldHistories)
                                    {
            
                                        if(histories.TryGetValue(oldHistory.table_name, out History? newHistory))
                                        {
                                            if(newHistory == null)
                                            {
                                                continue;
                                            }

                                            int cmp = DateTime.Compare(oldHistory.update_time, newHistory.update_time);
                                            if (cmp == 1 || cmp == 0)
                                            {
                                                histories.Remove(oldHistory.table_name, out History? outRemove);
                                            }
                                        }
                                    }
                                }
                            }

                            if (histories.Count <= 0)
                            {
                                throw new Exception("데이터베이스의 테이블에 수정사항이 존재하지 않음");
                            }

                            // 모두 덮어씌우기

                            string jsonString = JsonSerializer.Serialize(newHistories, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) });

                            File.WriteAllText($"{cachePath}/{Program.SchemaName}.json", jsonString, Encoding.UTF8);
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
                            string jsonString = JsonSerializer.Serialize(orderByTables, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) });

                            File.WriteAllText($"{jsonPath}/{Program.SchemaName}.json", jsonString, Encoding.UTF8);
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

            foreach (KeyValuePair<string, History> history in histories)
            {
                using (var connnection = new MySqlConnection(Program.ConnectionString))
                {
                    await connnection.OpenAsync();
                    Console.WriteLine("Task : " + history.Key);

                    //저장할 테이블 생성
                    Table table = new Table
                    {
                        DbTableName = history.Key,
                        Name = ParseTableName(history.Key, Program.TableNameSeparator),
                    };

                    //COLUMN (이름, 타입, NULLABLE, 디폴트) 검색
                    string getColumnsQuery = $@"
                            SELECT COLUMN_NAME, COLUMN_TYPE, IS_NULLABLE, COLUMN_DEFAULT, COLUMN_COMMENT
                            FROM information_schema.columns 
                            WHERE TABLE_NAME = '{history.Key}' AND TABLE_SCHEMA = SCHEMA();";


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
                                Comment = columnsReader.IsDBNull(4) ? "NONE COMMENT" : columnsReader.GetString(4),
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
                    { "tinyint", "bool" },
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
