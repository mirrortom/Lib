using Microsoft.Data.Sqlite;
using System;
using System.Data.Common;

namespace Lib.dbm
{
    public class SQLite : DBMO
    {
        public SQLite(string dbPath = null)
        {
            this.connString = dbPath ?? @"data source=e:\db\test.db";
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
