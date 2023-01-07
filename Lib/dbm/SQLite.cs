using Microsoft.Data.Sqlite;
using System;
using System.Data.Common;

namespace Lib.dbm
{
    public class SQLite : DBMO
    {
        /// <summary>
        /// 1. 数据库文件路径
        /// 2. ":memory:": 内存中的sqlite(暂时没实现,内存中数据库链接关闭时会删除) https://docs.microsoft.com/zh-cn/dotnet/standard/data/sqlite/in-memory-databases
        /// </summary>
        /// <param name="dbPath"></param>
        public SQLite(string dbPath = null)
        {
            string dbconstr = dbPath ?? @"e:\db\test.db";
            this.connString = $"data source={dbconstr}";

        }
        protected override void ConnInstance()
        {
            this.conn = new SqliteConnection();
        }

        protected override void CmdInstance(string sql)
        {
            this.cmd = new SqliteCommand(sql);
        }

        protected override DbParameter ParaInstance(string name, object val)
        {
            SqliteParameter para = new()
            {
                ParameterName = name,
                Value = val ?? DBNull.Value
            };
            return para;
        }

        protected override DbParameter OutParaInstance(string name, int dbType)
        {
            SqliteParameter para = new()
            {
                Direction = System.Data.ParameterDirection.Output,
                ParameterName = name,
                SqliteType = (SqliteType)dbType
            };
            return para;
        }
    }
}
