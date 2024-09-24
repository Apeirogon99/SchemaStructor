using SchemaStructor.Data;
using SchemaStructor.Format;
using System.Collections.Concurrent;
using System.Text.Json;

namespace SchemaStructor.Script
{
    /// <summary>
    /// 스키마 구조체 빌더
    /// </summary>
    public class StructBuilder
    {
        //Models 경로
        private string structOutputPath = string.Empty;
        private string enumFolderPath = string.Empty;
        private string modelsFolderPath = string.Empty;

        private ConcurrentQueue<Table> tables = new ConcurrentQueue<Table>();
        private object _writeScriptLock = new object();

        public StructBuilder() 
        {
            structOutputPath = Path.Combine(Program.StructOutputPath, Program.ProjectName);
            if (!Directory.Exists(structOutputPath))
            {
                Directory.CreateDirectory(structOutputPath);
            }

            {
                string enumPath = structOutputPath + "/Enum";
                if (!Directory.Exists(enumPath))
                {
                    Directory.CreateDirectory(enumPath);
                }

                enumFolderPath = enumPath + "/" + Program.SchemaName;
                if (!Directory.Exists(enumFolderPath))
                {
                    Directory.CreateDirectory(enumFolderPath);
                }
            }

            {
                string modelsPath = structOutputPath + "/Models";
                if (!Directory.Exists(modelsPath))
                {
                    Directory.CreateDirectory(modelsPath);
                }

                modelsFolderPath = modelsPath + "/" + Program.SchemaName;
                if (!Directory.Exists(modelsFolderPath))
                {
                    Directory.CreateDirectory(modelsFolderPath);
                }
            }
        }

        public void Build(int workThreadNumber)
        {
            try
            {
                DirectoryInfo? directoryInfo = Directory.GetParent(Environment.CurrentDirectory);
                if (directoryInfo == null || directoryInfo.Parent == null)
                {
                    throw new Exception("bin 폴더가 존재하지 않습니다.");
                }

                string jsonPath = directoryInfo.Parent.FullName + "\\Json";
                string[] jsonFilePaths = Directory.GetFiles(jsonPath, $"*.json");
                string? schemaFilePath = jsonFilePaths.FirstOrDefault(file => file.Contains($"{Program.SchemaName}.json"));
                if (schemaFilePath != null)
                {
                    string jsonContent = File.ReadAllText(schemaFilePath);
                    var jsonObj = JsonSerializer.Deserialize<ConcurrentQueue<Table>>(jsonContent);
                    if (jsonObj == null || jsonObj.Count == 0)
                    {
                        throw new Exception("Table이 없거나 Deserialize에 실패하였습니다.");
                    }
                    tables = jsonObj;

                    //테이블 이름 작성
                    string tableNames = "";
                    foreach (var table in tables)
                    {
                        tableNames += string.Format(EnumContextFormat.EnumContext, table.DbTableName);
                    }

                    DoBuild();

                    {
                        var enumText = string.Format(EnumFormat.context, Program.ProjectName, Program.SchemaName, tableNames);
                        File.WriteAllText($"{enumFolderPath}/{Program.SchemaName}TableName.cs", enumText);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error [StructorBuilder.Build] : " + ex.Message);
            }
            
        }

        public void DoBuild()
        {
            //테이블 구조체 작성
            while (tables.TryDequeue(out var table))
            {
                string tableName = table.Name;
                string structName = "F" + table.Name;

                string columnEnumContextRegister = "";
                string columnEnumValueContextRegister = "";

                string columnStructContextRegister = "";
                string columnStructValueContextRegister = "";


                foreach (Column column in table.Columns)
                {
                    if (column.Values != null)
                    {
                        string enumName = "E" + char.ToUpper(column.Name[0]) + column.Name.Substring(1);
                        foreach (string value in column.Values)
                        {
                            columnEnumValueContextRegister += string.Format(StructContextFormat.EnumValueContext, value);
                        }
                        columnEnumContextRegister += string.Format(StructContextFormat.EnumContext,
                            enumName,
                            columnEnumValueContextRegister);

                        columnStructValueContextRegister += string.Format(StructContextFormat.StructValueContext,
                            (column.Nullable == false) ? enumName : enumName + "?",
                            column.Name,
                            enumName + "." + column.Default);
                    }
                    else
                    {
                        columnStructValueContextRegister += string.Format(StructContextFormat.StructValueContext,
                            (column.Nullable == false) ? column.Type : column.Type + "?",
                            column.Name,
                            column.Default);
                    }


                }
                columnStructContextRegister =
                    string.Format(StructContextFormat.StructContext,
                    structName,
                    columnStructValueContextRegister);

                var structText = string.Format(StructFormat.context,
                    Program.ProjectName,
                    Program.SchemaName,
                    columnEnumContextRegister,
                    columnStructContextRegister);

                File.WriteAllText($"{modelsFolderPath}/{structName}.cs", structText);
            }
        }
    }
}
