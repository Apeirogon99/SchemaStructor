using MySqlConnector;
using System.Globalization;
using System.Text.Json;

namespace SchemaStructor.Schema
{
    public class Schema
    {
        private string connectionString;
        private MySqlConnection connection;

        public Schema(string connectionString)
        {
            this.connectionString = connectionString;
            this.connection = new MySqlConnection(connectionString);
        }

        public void Export()
        {
            try
            {
                this.connection.Open();

                string getSchemaNameQuery = "SELECT SCHEMA();";
                MySqlCommand getSchemaNameCommand = new MySqlCommand(getSchemaNameQuery, this.connection);

                string schemaName = string.Empty;
                using (MySqlDataReader schemaNameReader = getSchemaNameCommand.ExecuteReader())
                {

                    if(true == schemaNameReader.Read())
                    {
                        schemaName = schemaNameReader.GetString(0);
                    }
                }

                string getTablesNameQuery = "SELECT TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA = SCHEMA();";
                MySqlCommand getTablesNameCommand = new MySqlCommand(getTablesNameQuery, this.connection);

                List<string> tableNames = new List<string>();
                using (MySqlDataReader tablesNameReader = getTablesNameCommand.ExecuteReader())
                {
                    while (tablesNameReader.Read())
                    {
                        tableNames.Add(tablesNameReader.GetString(0));
                    }
                }

                List<Table> tables = new List<Table>();
                foreach (string tableName in tableNames)
                {
                    Table table = new Table();

                    table.Name = ParseTableName(tableName, "_");

                    string getColumnsQuery = $@"
                            SELECT COLUMN_NAME, COLUMN_TYPE, IS_NULLABLE, COLUMN_DEFAULT
                            FROM information_schema.columns 
                            WHERE TABLE_NAME = '{tableName}' AND TABLE_SCHEMA = SCHEMA();";
                    MySqlCommand getColumnsCommand = new MySqlCommand(getColumnsQuery, this.connection);

                    using (MySqlDataReader columnsReader = getColumnsCommand.ExecuteReader())
                    {
                        while (columnsReader.Read())
                        {
                            Column column = new Column();

                            column.Name = columnsReader.GetString(0);
                            column.Type = ConvertMySqlTypeToCSharp(columnsReader.GetString(1));
                            if(column.Type == "enum")
                            {
                                column.Values = ParseEnumValues(columnsReader.GetString(1));
                            }

                            column.Nullable = (columnsReader.GetString(2) == "YES");
                            column.Default = columnsReader.IsDBNull(3) ? "NULL" : columnsReader.GetString(3);

                            table.Columns.Add(column);
                        }
                    }
                    tables.Add(table);
                }

                string jsonPath = "";
                DirectoryInfo? directoryInfo = Directory.GetParent(Environment.CurrentDirectory);
                if(directoryInfo != null && directoryInfo.Parent != null)
                {
                    jsonPath = directoryInfo.Parent.FullName;
                }

                string jsonString = JsonSerializer.Serialize(tables, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText($"{jsonPath}/Json/{schemaName}.json", jsonString);

                this.connection.Close();

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error [Schema.Export] : " + ex.Message);
            }
        }

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
