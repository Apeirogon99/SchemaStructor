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
        private string projectName = "WebCommonLibrary";
        private string schemaName = "MasterDatabase";

        private ConcurrentQueue<Table>? tables = new ConcurrentQueue<Table>();
        private object _writeScriptLock = new object();


        private static string TablesContextRegister = "";
        private string EnumContextRegister = "";

        public ScriptBuilder() 
        {
            folderPath = Path.Combine(Program.OutputPath, "Script");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
        }

        public void Build(int workThreadNumber)
        {
            try
            {


                string jsonPath = "";
                DirectoryInfo? directoryInfo = Directory.GetParent(Environment.CurrentDirectory);
                if (directoryInfo != null && directoryInfo.Parent != null)
                {
                    jsonPath = directoryInfo.Parent.FullName + "/Json/";
                }

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
                                    column.Default);
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

                        var structText = string.Format(StructFormat.context, projectName, columnEnumContextRegister, columnStructContextRegister);
                        File.WriteAllText($"{folderPath}/{structName}.cs", structText);
                    }
                }

                //스크립트 최종 생성
                {




                    var enumText = string.Format(EnumFormat.context, projectName, EnumContextRegister);
                    File.WriteAllText($"{folderPath}/{schemaName}TableName.cs", enumText);

                    var interfaceText = string.Format(InterfaceFormat.context, projectName, schemaName, TablesContextRegister);
                    File.WriteAllText($"{folderPath}/I{schemaName}Context.cs", interfaceText);
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
