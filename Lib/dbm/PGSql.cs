using System;
using System.Data.Common;
using Npgsql;

namespace Lib.dbm
{
    public class PGSql : DBMO
    {
        public PGSql(string connectionString = null)
        {
            this.connString = connectionString ?? @"server=localhost;username=postgres;database=test;port=5432;password=123456";
        }
        protected override void ConnInstance()
        {
            this.conn = new NpgsqlConnection();
        }
        protected override void CmdInstance(string sql)
        {
            this.cmd = new NpgsqlCommand(sql);
        }
        protected override DbParameter ParaInstance(string name, object val)
        {
            NpgsqlParameter para = new()
            {
                ParameterName = name,
                Value = val ?? DBNull.Value
            };
            return para;
        }
        protected override DbParameter OutParaInstance(string name, int dbType)
        {
            NpgsqlParameter para = new()
            {
                Direction = System.Data.ParameterDirection.Output,
                ParameterName = name,
                NpgsqlDbType = (NpgsqlTypes.NpgsqlDbType)dbType
            };
            return para;
        }

    }
}
