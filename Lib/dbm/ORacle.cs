using Oracle.ManagedDataAccess.Client;
using System;
using System.Data.Common;

namespace Lib.dbm;

public class ORacle : DBMO
{
    public ORacle(string connectionString = null)
    {
        this.connString = connectionString ?? @"Data Source=oracletest;user id=system;password=123456";
        // 不在本机要用这个
        //this.ConnectionString = "User ID=system;Password=12345678;Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=192.168.1.10)(PORT=1521)))(CONNECT_DATA=(SERVICE_NAME=oracletest)))";
        // oraclesql参数匹配正则.不匹配 :1 匹配 :a_1 命名规则:一个:开头,,1字母,后面字母数字.
        // 注意:(冒号)参数前缀,有可能是oracle时间格式化函数的':',所以不要在sql语句中使用其它有:号语句
        this.paraRege = @":[a-zA-Z]+[a-zA-Z\d_]*";
        this.paraPrefixChar = ':';
    }
    protected override void ConnInstance()
    {
        this.conn = new OracleConnection();
    }

    protected override void CmdInstance(string sql)
    {
        this.cmd = new OracleCommand(sql);
    }

    protected override DbParameter ParaInstance(string name, object val)
    {
        OracleParameter para = new()
        {
            ParameterName = name,
            Value = val ?? DBNull.Value
        };
        return para;
    }
    protected override DbParameter OutParaInstance(string name, int dbType)
    {
        OracleParameter para = new()
        {
            Direction = System.Data.ParameterDirection.Output,
            ParameterName = name,
            OracleDbType = (OracleDbType)dbType
        };
        return para;
    }
}
