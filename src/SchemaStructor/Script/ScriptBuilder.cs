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
    /// <summary>
    /// 스키마 스크립트 빌더
    /// </summary>
    public class ScriptBuilder
    {

        private string reposiotryOutputPath = string.Empty;

        private string reposiotryFolderPath = string.Empty;
        private string reposiotryContextFolderPath = string.Empty;
        private string interfaceFolderPath = string.Empty;

        //ScheamContext
        private string InterfaceContextRegister = string.Empty;
        private string SchemaRegionContextRegister = string.Empty;
        private string SchemaConstructContextRegister = string.Empty;
        private string SchemaLoadTableContextRegister = string.Empty;
        private string SchemaIsValidContextRegister = string.Empty;

        private ConcurrentQueue<Table> tables = new ConcurrentQueue<Table>();
        private object _writeScriptLock = new object();

        public ScriptBuilder() 
        {
            reposiotryOutputPath = Path.Combine(Program.ReposiotryOutputPath);
            if (!Directory.Exists(reposiotryOutputPath))
            {
                Directory.CreateDirectory(reposiotryOutputPath);
            }

            {
                reposiotryFolderPath = reposiotryOutputPath + "/Reposiotry";
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
                string? schemaFilePath = jsonFilePaths.FirstOrDefault(file => file == $"{Program.SchemaName}.json");
                if (schemaFilePath != null)
                {
                    string jsonContent = File.ReadAllText(schemaFilePath);
                    var jsonObj = JsonSerializer.Deserialize<ConcurrentQueue<Table>>(jsonContent);
                    if (jsonObj == null || jsonObj.Count == 0)
                    {
                        throw new Exception("Table이 없거나 Deserialize에 실패하였습니다.");
                    }
                    tables = jsonObj;

                    //인터페이스 테이블 작성
                    {
                        foreach (var table in tables)
                        {
                            InterfaceContextRegister += string.Format(InterfaceContextFormat.TablesContext, table.Name, "F" + table.Name);
                        }
                    }

                    DoBuild();

                    //스크립트 최종 생성
                    {
                        var dbTable = string.Format(DbTableFormat.context,
                            Program.ProjectName,
                            Program.SchemaName);
                        File.WriteAllText($"{reposiotryFolderPath}/DbTable.cs", dbTable);

                        var dbContext = string.Format(DbContextFormat.context,
                            Program.ProjectName,
                            Program.SchemaName);
                        File.WriteAllText($"{reposiotryFolderPath}/DbContext.cs", dbContext);

                        var schemaDbContext = string.Format(SchemaDbContextFormat.context,
                            Program.ProjectName,
                            Program.SchemaName,
                            SchemaRegionContextRegister,
                            SchemaConstructContextRegister,
                            SchemaLoadTableContextRegister,
                            SchemaIsValidContextRegister);
                        File.WriteAllText($"{reposiotryContextFolderPath}/{Program.SchemaName}Context.cs", schemaDbContext);

                        var interfaceText = string.Format(InterfaceFormat.context,
                            Program.ProjectName,
                            Program.SchemaName,
                            InterfaceContextRegister);
                        File.WriteAllText($"{interfaceFolderPath}/I{Program.SchemaName}Context.cs", interfaceText);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error [ScriptBuilder.Build] : " + ex.Message);
            }
        }

        public void DoBuild()
        {
            string isValidColumnContextRegister = ""; //IsValid에 들어갈 테이블
            while (tables.TryDequeue(out var table))
            {
                string tableName = table.Name;
                string structName = "F" + table.Name;

                SchemaRegionContextRegister += string.Format(SchemaContextFormat.RegionContext, structName, tableName);
                SchemaConstructContextRegister += string.Format(SchemaContextFormat.ConstructContext, tableName);

                string columnNamesInQueryContext = "";  //쿼리 안에 들어갈 컬럼 이름
                foreach (Column column in table.Columns)
                {
                    columnNamesInQueryContext += string.Format(SchemaContextFormat.ColumnNamesContext, column.Name);
                }
                columnNamesInQueryContext = columnNamesInQueryContext.Substring(0, columnNamesInQueryContext.Length - 2);

                string queryContext = string.Format(SchemaContextFormat.QueryContext, columnNamesInQueryContext, table.DbTableName);
                SchemaLoadTableContextRegister += string.Format(SchemaContextFormat.LoadTableContext, structName, tableName, queryContext);

                isValidColumnContextRegister += string.Format(SchemaContextFormat.IsValidColumnContext, tableName);
            }

            //테이블 존재하는지 확인
            SchemaIsValidContextRegister += string.Format(SchemaContextFormat.IsValidContext, isValidColumnContextRegister);
        }
    }
}
