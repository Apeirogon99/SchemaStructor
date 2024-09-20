using SchemaStructor.Data;
using SchemaStructor.Format;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SchemaStructor.Script
{
    public class ScriptBuilder
    {

        private string folderPath = string.Empty;
        private string reposiotryFolderPath = string.Empty;
        private string reposiotryContextFolderPath = string.Empty;
        private string interfaceFolderPath = string.Empty;
        private string enumFolderPath = string.Empty;
        private string modelsFolderPath = string.Empty;

        private ConcurrentQueue<Table>? tables = new ConcurrentQueue<Table>();
        private object _writeScriptLock = new object();


        private static string TablesContextRegister = "";
        private string EnumContextRegister = "";

        public ScriptBuilder() 
        {
            folderPath = Path.Combine(Program.OutputPath, Program.ProjectName);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            {
                reposiotryFolderPath = folderPath + "/Reposiotry";
                if (!Directory.Exists(reposiotryFolderPath))
                {
                    Directory.CreateDirectory(reposiotryFolderPath);
                }

                reposiotryContextFolderPath = reposiotryFolderPath + "/" + Program.SchemaName;
                if (!Directory.Exists(reposiotryContextFolderPath))
                {
                    Directory.CreateDirectory(reposiotryContextFolderPath);
                }

                interfaceFolderPath = reposiotryFolderPath + "/Interfaces";
                if (!Directory.Exists(interfaceFolderPath))
                {
                    Directory.CreateDirectory(interfaceFolderPath);
                }
            }

            {
                string enumPath = folderPath + "/Enum";
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
                string modelsPath = folderPath + "/Models";
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
                string[] jsonFilePaths = Directory.GetFiles(jsonPath, "*.json");
                foreach (string filePath in jsonFilePaths)
                {
                    string jsonContent = File.ReadAllText(filePath);

                    tables = JsonSerializer.Deserialize<ConcurrentQueue<Table>>(jsonContent);
                    if (tables == null || tables.Count == 0)
                    {
                        continue;
                    }

                    while (tables.TryDequeue(out var table))
                    {
                        string tableName = table.Name;
                        string structName = "F" + table.Name;

                        EnumContextRegister += string.Format(EnumContextFormat.EnumContext, tableName);
                        TablesContextRegister += string.Format(InterfaceContextFormat.TablesContext, structName, tableName);

                        string columnEnumContextRegister = "";
                        string columnEnumValueContextRegister = "";

                        string columnStructContextRegister = "";
                        string columnStructValueContextRegister = "";
                        foreach (Column column in table.Columns)
                        {
                            if(column.Values != null)
                            {
                                foreach (string value in column.Values)
                                {
                                    columnEnumValueContextRegister += string.Format(StructContextFormat.EnumValueContext, value);
                                }
                                string enumName = "E" + CapitalizeFirstLetter(column.Name);
                                columnEnumContextRegister += string.Format(StructContextFormat.EnumContext, enumName, columnEnumValueContextRegister);

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
                        columnStructContextRegister = string.Format(StructContextFormat.StructContext, structName, columnStructValueContextRegister);

                        var structText = string.Format(StructFormat.context, Program.ProjectName, Program.SchemaName, columnEnumContextRegister, columnStructContextRegister);
                        File.WriteAllText($"{modelsFolderPath}/{structName}.cs", structText);
                    }
                }

                //스크립트 최종 생성
                {
                    var dbTable = string.Format(DbTableFormat.context, Program.ProjectName, Program.SchemaName);
                    File.WriteAllText($"{reposiotryFolderPath}/DbTable.cs", dbTable);

                    var dbContext = string.Format(DbContextFormat.context, Program.ProjectName, Program.SchemaName);
                    File.WriteAllText($"{reposiotryFolderPath}/DbContext.cs", dbContext);

                    var enumText = string.Format(EnumFormat.context, Program.ProjectName, Program.SchemaName, EnumContextRegister);
                    File.WriteAllText($"{enumFolderPath}/{Program.SchemaName}TableName.cs", enumText);

                    var schemaDbContext = string.Format(SchemaContextFormat.context, Program.ProjectName, Program.SchemaName);
                    File.WriteAllText($"{reposiotryContextFolderPath}/{Program.SchemaName}Context.cs", schemaDbContext);

                    var interfaceText = string.Format(InterfaceFormat.context, Program.ProjectName, Program.SchemaName, TablesContextRegister);
                    File.WriteAllText($"{interfaceFolderPath}/I{Program.SchemaName}Context.cs", interfaceText);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error [ScriptBuilder.Build] : " + ex.Message);
            }
            
        }

        private string CapitalizeFirstLetter(string value)
        {
            return char.ToUpper(value[0]) + value.Substring(1);
        }

    }
}
