using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net.Http.Headers;

namespace SchemaStructor.Format
{
    //public sealed class MasterDatabaseContext : DbContext, IMasterDatabaseContext
    //{
    //    private readonly string _connectionString;
    //    #region DatabaseContext 
    //    public DbTable<FMasterRewardBase> RewardBase { get; }
    //    #endregion

    //    public MasterDatabaseContext(string connectionString)
    //    {
    //        _connectionString = connectionString;

    //        RewardBase = GetMasterRewardBase().Result;
    //    }

    //    public async Task<DbTable<FMasterRewardBase>> GetMasterRewardBase()
    //    {
    //        string query = "";
    //        return await LoadDatabaseTable<FMasterRewardBase>(_connectionString, query);
    //    }

    //    protected override bool IsValidContext()
    //    {
    //        if (RewardBase.IsValid() == false ||
    //            RewardBase.IsValid() == false ||
    //            false)
    //        {
    //            return false;
    //        }

    //        return true;
    //    }

    //}

    internal class SchemaDbContextFormat
    {
        public static string context =
            @"
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using {0}.Reposiotry;
using {0}.Models.{1};
using {0}.Reposiotry.Interfaces;

namespace {0}.Reposiotry.{1}
{{
    public class {1}Context : DbContext, I{1}Context
    {{
        private readonly string _connectionString;

        #region DatabaseContext{2}
        #endregion

        public MasterDatabaseContext(string connectionString)
        {{
            _connectionString = connectionString;
            {3}
        }}

        public void Dispose()
        {{

        }}

        {4}

        {5}
    }}
}}
            ";
    }

    internal class SchemaContextFormat
    {
        /// <summary>
        /// {0} 구조체 테이블 이름
        /// {1} 테이블 이름
        /// </summary>
        public static string RegionContext =
            @"
        public DbTable<{0}> {1} {{ get; }}";

        /// <summary>
        /// {0} 테이블 이름
        /// </summary>
        public static string ConstructContext =
            @"
            {0} = Load{0}().Result;";

        /// <summary>
        /// {0} 구조체 이름
        /// {1} 테이블 이름
        /// {2} 쿼리
        /// </summary>
        public static string LoadTableContext =
            @"
        private async Task<DbTable<{0}>> Load{1}()
        {{
            string query = {2};
            return await LoadDatabaseTable<{0}>(_connectionString, query);
        }}
            ";

        /// <summary>
        /// {0} 컬럼 이름들
        /// {1} 데이터베이스에 저장된 테이블 이름
        /// </summary>
        public static string QueryContext =
            @"""SELECT {0} FROM {1};""";

        /// <summary>
        /// {0} 테이블 이름
        /// </summary>
        public static string ColumnNamesContext =
            @"{0}, ";

        /// <summary>
        /// {0} ValidColumns
        /// </summary>
        public static string IsValidContext =
            @"
        public override bool IsValidContext()
        {{
            if ({0}
                false)
            {{
                return false;
            }}

            return true;
        }}
            ";

        /// <summary>
        /// {0} 테이블 이름
        /// </summary>
        public static string IsValidColumnContext =
            @" 
                {0}.IsValid() == false ||";
    }
}
