using System;
using System.IO;
using SchemaStructor.Data;
using SchemaStructor.Script;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;

class Program
{

    public static string ConnectionString = string.Empty;
    public static string StructOutputPath = string.Empty;
    public static string ReposiotryOutputPath = string.Empty;
    public static string ProjectName = string.Empty;
    public static string SchemaName = string.Empty;
    public static string TableNameSeparator = string.Empty;

    static void Main(string[] args)
    {
        //AppSettings.Json 읽어 오기
        {
            var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

            if (config == null)
            {
                return;
            }

            ConnectionString = config["ApplicationSettings:ConnectionString"] ?? string.Empty;

            ProjectName = config["ApplicationSettings:ProjectName"] ?? string.Empty;
            SchemaName = config["ApplicationSettings:SchemaName"] ?? string.Empty;
            TableNameSeparator = config["ApplicationSettings:TableNameSeparator"] ?? string.Empty;

            StructOutputPath = config["ApplicationSettings:StructOutputPath"] ?? string.Empty;
            ReposiotryOutputPath = config["ApplicationSettings:ReposiotryOutputPath"] ?? string.Empty;
        }

        int workThreadNumber = 1;
        if(ConnectionString != string.Empty && ProjectName != string.Empty && SchemaName != string.Empty)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            
            Schema schema = new Schema();
            schema.Export(workThreadNumber);

            stopwatch.Stop();
            Console.WriteLine($"Schema export elapsed time: {stopwatch.ElapsedMilliseconds} ms");
        }
        else
        {
            return;
        }

        if(StructOutputPath != string.Empty)
        {
            Stopwatch stopwatch = new Stopwatch();

            stopwatch.Start();

            StructBuilder structBuilder = new StructBuilder();
            structBuilder.Build(workThreadNumber);

            stopwatch.Stop();
            Console.WriteLine($"Struct builder elapsed time: {stopwatch.ElapsedMilliseconds} ms");
        }

        if (ReposiotryOutputPath != string.Empty)
        {
            Stopwatch stopwatch = new Stopwatch();

            stopwatch.Start();

            ScriptBuilder scriptBuilder = new ScriptBuilder();
            scriptBuilder.Build();

            stopwatch.Stop();
            Console.WriteLine($"Script builder elapsed time: {stopwatch.ElapsedMilliseconds} ms");
        }

    }
}