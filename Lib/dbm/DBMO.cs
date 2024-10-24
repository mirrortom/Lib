using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using dbLog = Lib.NLogHelp;
namespace Lib.dbm;

/// <summary>
/// 数据库管理CRUD操作. Database Management Operator
/// </summary>
abstract public class DBMO
{
    #region 成员

    /// <summary>
    /// sql参数名字前缀符号.默认@,oracle要用:
    /// </summary>
    protected char paraPrefixChar = '@';

    /// <summary>
    /// sql参数匹配正则
    /// 默认值适用于sqlserver,maria,sqlite
    /// 命名规则:一个@开头,1字母,后面字母数字.
    /// 该正则 不匹配 @@xxx @1 匹配@a_1.
    /// </summary>
    protected string paraRege = @"(?<!@)@[a-zA-Z]+[a-zA-Z\d_]*";

    /// <summary>
    /// 当前连接串
    /// </summary>
    protected string connString;

    /// <summary>
    /// 当前连接
    /// </summary>
    protected DbConnection conn;

    /// <summary>
    /// 当前命令
    /// </summary>
    protected DbCommand cmd;

    /// <summary>
    /// 当前事务
    /// </summary>
    protected DbTransaction tran;

    /// <summary>
    /// 是否在事务中执行.标识
    /// </summary>
    private bool inTran = false;
    /// <summary>
    /// 表示异常信息
    /// </summary>
    private string message;

    #endregion

    #region 属性,外部调用
    /// <summary>
    /// 记录执行日志(sql和参数),默认只有出错时才记录
    /// </summary>
    public bool OpenLog { get; set; } = false;
    /// <summary>
    /// 每个执行的sql可以有一个日志id号.指示这个执行是属于一个以该guid为编号的业务流程的.
    /// 可以用guid,默认为空字符串
    /// </summary>
    public string LogOrder { get; set; } = string.Empty;
    #endregion

    #region 抽象方法由具体数据库对象类实现

    /// <summary>
    /// 辅助方法: 实例化db DbConnection对象
    /// </summary>
    /// <returns></returns>
    abstract protected void ConnInstance();

    /// <summary>
    /// 辅助方法: 实例化sql命令对象
    /// </summary>
    abstract protected void CmdInstance(string sql);

    /// <summary>
    /// 辅助方法: 实例化sql参数对象,
    /// </summary>
    abstract protected DbParameter ParaInstance(string name, object val);

    /// <summary>
    /// 辅助方法: 实例化sql参数对象,为了存储过程的传出参数
    /// </summary>
    abstract protected DbParameter OutParaInstance(string name, int dbType);

    #endregion

    #region 开启与关闭数据库连接

    /// <summary>
    /// 打开一个连接
    /// </summary>
    private void OpenDB()
    {
        // 连接未建立(含首次执行任务,在或者不在事务中的情况):建立连接对象,打开连接
        if (this.conn == null)
        {
            // 建立链接对象,打开连接
            this.ConnInstance();
            this.conn.ConnectionString = this.connString;
            this.conn.Open();
        }
        // 如果开启了事务,可能会多次执行任务,事务对象只在首次执行任务时建立一次
        if (this.inTran == true)
        {
            // 事务开启时,首次执行命令会建立事务对象
            this.tran ??= this.conn.BeginTransaction();
            // 命令装载到这个事务
            this.cmd.Transaction = this.tran;
        }
        // 命令装载到这个连接
        this.cmd.Connection = this.conn;
    }
    /// <summary>
    /// 关闭数据库(类内用于自动关闭使用)
    /// 条件解释:连接存在,当前是状态打开,并且不是事务状态(事物状态时,在提交和回滚时关闭)
    /// </summary>
    private void CloseDB()
    {
        // 如果执行有错误,或者开启了日志,记录日志
        if (this.message != null || OpenLog == true)
        {
            // 参数对信息
            StringBuilder paras = new();
            for (int i = 0; i < this.cmd.Parameters.Count; i++)
            {
                paras.Append($"{this.cmd.Parameters[i]}={this.cmd.Parameters[i].Value} | ");
            }
            string msg = this.message ?? "Success!";
            dbLog.DBLog($"提示信息:[{msg}] 任务id:[{this.LogOrder}] {Environment.NewLine}SQL:[{this.cmd.CommandText}]{Environment.NewLine}参数:[{paras}]");
        }
        // 连接开启时,并且不在事务中进行时,表示任务完成,可以释放资源了.
        if (this.conn != null && this.tran == null)
        {
            // 释放连接资源
            this.conn.Close();
            this.conn.Dispose();
            this.conn = null;
            this.cmd.Dispose();
            this.cmd = null;
            //
            this.inTran = false;
            this.LogOrder = default;
            this.message = null;
        }
    }
    #endregion

    #region 执行查询,外部调用
    /// <summary>
    /// 执行查询[数组式参数] [字典数组式结果集],字段名是键.无值或出错返回null
    /// <para>参数占位标记的顺序和参数数组元素顺序保持一致</para>
    /// <para>至少写两个参数(可加一个无用参数)否则将会判定到泛型重载.</para>
    /// <para>如果参数就是数组,强制object类型,例如 (object)string[]{3}</para>
    /// </summary>
    public Dictionary<string, object>[] ExecuteQuery(string sql, params object[] paras)
    {
        return this.InItCmd(sql, paras) ? this.Select() : null;
    }
    /// <summary>
    /// 执行查询[字典式参数] [字典数组式结果集],字段名是键.无值或出错返回null
    /// <para>字典键与参数名字相同(注意大小写也相同),不带@或者:前缀符号</para>
    /// </summary>
    public Dictionary<string, object>[] ExecuteQuery(string sql, Dictionary<string, object> parasdict)
    {
        return this.InItCmd(sql, parasdict) ? this.Select() : null;
    }

    /// <summary>
    /// 执行查询[对象式参数] [字典数组式结果集],字段名是键.无值或出错返回null
    /// <para>sql参数值对象P,参数对象的属性或者成员名称必须和参数名字一样,大小写不限.</para>
    /// </summary>
    public Dictionary<string, object>[] ExecuteQuery<P>(string sql, P paraEntity)
    {
        return this.InItCmd<P>(sql, paraEntity) ? this.Select() : null;
    }

    /// <summary>
    /// 执行查询[数组式参数] [实体对象数组式结果集],没有值返回null
    /// <para>E类型是实体对象,成员或属性名与查询语句字段的别名一样,大小写不限.C#默认构造函数和字段值</para>
    /// <para>如果对应字段的数据值是DBNULL,那么T的该字段/属性将设置C#系统默认值.</para>
    /// </summary>
    public E[] ExecuteQuery<E>(string sql, params object[] paras) where E : new()
    {
        return this.InItCmd(sql, paras) ? this.Select<E>() : null;
    }
    /// <summary>
    /// 执行查询[字典式参数] [实体对象数组式结果集],没有值返回null
    /// </summary>
    public E[] ExecuteQuery<E>(string sql, Dictionary<string, object> parasdict) where E : new()
    {
        return this.InItCmd(sql, parasdict) ? this.Select<E>() : null;
    }
    /// <summary>
    /// 执行查询[对象式参数] [实体对象数组式结果集],没有值返回null
    /// <para>sql参数值对象P,参数对象的属性或者成员名称必须和参数名字一样,大小写不限.</para>
    /// </summary>
    public E[] ExecuteQuery<E, P>(string sql, P paraentity) where E : new()
    {
        return this.InItCmd<P>(sql, paraentity) ? this.Select<E>() : null;
    }

    /// <summary>
    /// 执行查询[数组式参数] [单一结果值],无值或发生异常都返回null
    /// </summary>
    public object ExecuteScalar(string sql, params object[] paras)
    {
        return this.InItCmd(sql, paras) ? this.SelectScalar() : null;
    }

    /// <summary>
    /// 执行查询[字典式参数] [单一结果值],无值或发生异常都返回null
    /// </summary>
    public object ExecuteScalar(string sql, Dictionary<string, object> parasdict)
    {
        return this.InItCmd(sql, parasdict) ? this.SelectScalar() : null;
    }

    /// <summary>
    /// 执行查询[对象式参数] [单一结果值],无值或发生异常都返回null
    /// </summary>
    public object ExecuteScalar<P>(string sql, P paraentity)
    {
        return this.InItCmd<P>(sql, paraentity) ? this.SelectScalar() : null;
    }

    /// <summary>
    /// 执行非查询[数组式参数],返回受影响的行数,发生错误返回-999
    /// </summary>
    public int ExecuteNoQuery(string sql, params object[] paras)
    {
        return this.InItCmd(sql, paras) ? this.SelectNon() : -999;
    }

    /// <summary>
    /// 执行非查询[字典式参数],返回受影响的行数,发生异常返回-999
    /// </summary>
    public int ExecuteNoQuery(string sql, Dictionary<string, object> parasdict)
    {
        return this.InItCmd(sql, parasdict) ? this.SelectNon() : -999;
    }

    /// <summary>
    /// 执行非查询[对象式参数],返回受影响的行数,发生异常返回-999
    /// </summary>
    public int ExecuteNoQuery<P>(string sql, P paraentity)
    {
        return this.InItCmd<P>(sql, paraentity) ? this.SelectNon() : -999;
    }

    /// <summary>
    /// 执行INSERT[数组式参数],返回受影响行数,发生异常返回-999
    /// <para>INSERT语句不需要写VALUES部分.程序将自动补上,否则出错.</para>
    /// <para>例: insert into tab(col1,col2,..) </para>
    /// </summary>
    public int Insert(string sqlhalf, params object[] paras)
    {
        string sql = DBMO.AutoCmptInsertSql(sqlhalf, this.paraPrefixChar);
        if (sql == null) return -999;
        return this.InItCmd(sql, paras) ? this.SelectNon() : -999;
    }

    /// <summary>
    /// 执行INSERT[字典式参数],返回受影响行数,发生异常返回-999
    /// </summary>
    public int Insert(string insertHalf, Dictionary<string, object> parasdict)
    {
        string sql = DBMO.AutoCmptInsertSql(insertHalf, this.paraPrefixChar);
        if (sql == null) return -999;
        return this.InItCmd(sql, parasdict) ? this.SelectNon() : -999;
    }

    /// <summary>
    /// 执行INSERT[对象式参数],返回受影响行数,发生异常返回-999
    /// </summary>
    public int Insert<P>(string insertHalf, P paraentity)
    {
        string sql = DBMO.AutoCmptInsertSql(insertHalf, this.paraPrefixChar);
        if (sql == null) return -999;
        return this.InItCmd<P>(sql, paraentity) ? this.SelectNon() : -999;
    }

    /// <summary>
    /// 执行UPDATE[数组式参数],返回受影响行数,发生异常返回-999
    /// <para>UPDATE语句不需要写SET部分.写出要赋值的字段.程序将自动补齐SET部分否则出错</para>
    /// <para>例: update tab (col1,col2,...) where id=1</para>
    /// </summary>
    public int Update(string sqlhalf, params object[] paras)
    {
        string sql = DBMO.AutoCmptUpdateSql(sqlhalf, this.paraPrefixChar);
        if (sql == null) return -999;
        return this.InItCmd(sql, paras) ? this.SelectNon() : -999;
    }
    /// <summary>
    /// 执行UPDATE[字典式参数],返回受影响行数,发生异常返回-999
    /// </summary>
    public int Update(string sqlhalf, Dictionary<string, object> parasdict)
    {
        string sql = DBMO.AutoCmptUpdateSql(sqlhalf, this.paraPrefixChar);
        if (sql == null) return -999;
        return this.InItCmd(sql, parasdict) ? this.SelectNon() : -999;
    }
    /// <summary>
    /// 执行UPDATE[对象式参数],返回受影响行数,发生异常返回-999
    /// </summary>
    /// <returns></returns>
    public int Update<P>(string sqlhalf, P paraentity)
    {
        string sql = DBMO.AutoCmptUpdateSql(sqlhalf, this.paraPrefixChar);
        if (sql == null) return -999;
        return this.InItCmd<P>(sql, paraentity) ? this.SelectNon() : -999;
    }
    #endregion

    #region 执行存储过程

    /// <summary>
    /// 执行存储过程.返回受影响行数.异常返回-999
    /// proc:过程名 paradict:输入参数字典(无值传null) outparadict:输出参数类型字典(无值传null) outparavaluedict:输出参数结果值字典引用(无值传丢弃out _)
    /// </summary>
    /// <param name="proc">过程名sql</param>
    /// <param name="paradict">输入参数字典.</param>
    /// <param name="outparadict">输出参数类型字典,键是参数名,值是类型.值使用int,由对应的库类型转化.比如mysql,(int)MySql.Data.MySqlClient.MySqlDbType.Int32.其它数据库使用对应的类型枚举xxxxDbType</param>
    /// <param name="outparavaluedict">输出参数结果值字典引用.如果没有输出参数,传舍弃元 out _</param>
    /// <returns></returns>
    public int ExecuteProcedure(string proc, Dictionary<string, object> paradict,
        Dictionary<string, int> outparadict, out Dictionary<string, object> outparavaluedict)
    {
        // 初始化命令
        this.InItCmdProc(proc, paradict, outparadict);
        outparavaluedict = [];
        return this.Procedure(outparavaluedict);
    }

    #endregion

    #region 事务初始化和执行
    // 开始事务方法和提交事务方法.如果需要在事务中进行,应先调用此方法,最后调用提交事务

    /// <summary>
    /// 打开一个事务,成功返回真(该操作不会关闭数据库连接,请在后续使用"提交"或者"回滚").
    /// 执行这个方法后,做了一个要使用事务的标志,但没有真的打开事务.在命令执行前才会打开事务.
    /// </summary>
    public void BeginTransaction()
    {
        this.inTran = true;
    }
    /// <summary>
    /// 回滚一个事务.在执行不达预期时可调用此方法撤回执行.(完成后数据连接会关闭)
    /// </summary>
    /// <returns></returns>
    public bool RollBackTransaction()
    {
        try
        {
            // 如果开始事务后,没有执行任何操作,然后调用回滚,会报错.this.tran是null
            // 这里直接返回.
            if (this.tran == null) return true;
            this.tran.Rollback();
            return true;
        }
        catch (Exception e)
        {
            this.message = e.Message;
            return false;
        }
        finally
        {
            if (this.tran != null)
                this.tran.Dispose();
            this.tran = null;
            this.CloseDB();
        }
    }
    /// <summary>
    /// 提交事务,成功返回true,发生异常时回滚(完成后数据连接会关闭)
    /// </summary>
    public bool CommitTransaction()
    {
        try
        {
            // 如果调用beginTransaction()后,没有执行任何操作,然后调用了commit,
            // 那么this.tran是null会报错,这里直接返回!
            if (this.tran == null) return true;
            this.tran.Commit();
            return true;
            //
        }
        catch (Exception e)
        {
            this.tran.Rollback();
            this.message = e.Message;
            return false;
        }
        finally
        {
            if (this.tran != null)
                this.tran.Dispose();
            this.tran = null;
            this.CloseDB();
        }
    }
    #endregion

    #region sql命令和参数初始化 [内部方法]

    // 1.数组参数:sql语句中的参数占位变量与参数值数组按位置一一对应,例如参数 @a,@b,参数数组值
    //   ["vala","valb"],那么@a对应vala,@b对应valb.
    // 2.对象参数:sql语句中的参数名字(去掉@或者:参数名字前缀),与参数值对象字段或者属性名对应
    //   例如参数 @a,@b,参数对象值 {a=1,b=2},那么@a对应1,@b对应2
    // 3.字典参数:与对象参数类似,sql语句中的参数名字与字典参数值的键名对应
    //   例如参数@a,@b,字典参数值{a:"a",b:"b"},那么@a对应"a",@b对应"b"
    // 4.存储过程的输入输出参数,只支持字典参数

    /// <summary>
    /// 初始化命令,添加参数.[数组式参数],失败返回false,并记录日志
    /// <para>参数1:查询语句</para>
    /// <para>参数2:参数值数组.长度不能少于参数个数</para>
    /// </summary>
    private bool InItCmd(string sql, object[] paras)
    {
        string[] paraNames = NewCmdAndParseParaNames(sql);
        if (paraNames.Length == 0) return true;

        // 参数个数少于占位符个数
        if (paraNames.Length > paras.Length)
        {
            dbLog.DBLog($"错误!数组参数个数少于参数占位符!SQL: [{sql}]");
            return false;
        }
        for (int i = 0; i < paraNames.Length; i++)
        {
            NewParaAddCmd(paraNames[i], paras[i]);
        }
        return true;
    }

    /// <summary>
    /// 初始化命令,添加参数.[字典式参数]
    /// <para>参数1:查询语句</para>
    /// <para>参数2:参数值字典</para>
    /// </summary>
    private bool InItCmd(string sql, Dictionary<string, object> parasdict)
    {
        string[] paraNames = NewCmdAndParseParaNames(sql);
        if (paraNames.Length == 0) return true;

        if (parasdict == null || paraNames.Length > parasdict.Count)
        {
            dbLog.DBLog($"错误!字典参数值个数少于参数占位符!SQL: [{sql}]");
            return false;
        }
        for (int i = 0; i < paraNames.Length; i++)
        {
            string nameItem = paraNames[i];
            string key = paraNames[i][1..];
            // 如果参数字典里有对应参数名字的成员,则加入参数.
            if (parasdict.TryGetValue(key, out object value))
            {
                NewParaAddCmd(nameItem, value);
                continue;
            }
            dbLog.DBLog($"错误!字典参数占位符 [{nameItem}] 未提供值!SQL: [{sql}]");
            return false;
        }
        return true;
    }
    /// <summary>
    /// 初始化命令,添加参数.[对象式参数]
    /// <para>初始化命令:根据传入对象字段/属性名和参数名匹配,自动化查找参数值</para>
    /// <para>注意:对象字段/属性名和数据库字段名必须一样,不区分大小写.</para>
    /// <para>参数1:查询语句</para>
    /// <para>参数2:对象实例,该实例有与参数名匹配的字段/属性,并且已赋值.</para>
    /// </summary>
    private bool InItCmd<P>(string sql, P paraEntity)
    {
        string[] paraNames = NewCmdAndParseParaNames(sql);
        if (paraNames.Length == 0) return true;
        if (paraEntity == null)
        {
            dbLog.DBLog($"错误!实体参数对象不能是null!SQL: [{sql}]");
            return false;
        }

        for (int i = 0, len = paraNames.Length; i < len; i++)
        {
            string nameItem = paraNames[i];
            string memberName = nameItem[1..];
            object paraVal = null;
            // 先找成员
            bool hasMember = DBMO.FieldScan<P>(memberName, paraEntity, (field) =>
            {
                paraVal = field.GetValue(paraEntity);
            });
            // 再找属性
            if (hasMember == false)
            {
                hasMember = DBMO.PropScan<P>(memberName, paraEntity, (prop) =>
                {
                    paraVal = prop.GetValue(paraEntity);
                });
            }
            if (hasMember == false)
            {
                dbLog.DBLog($"错误!实体对象参数占位符 [{nameItem}] 未提供值!SQL: [{sql}]");
                return false;
            }
            NewParaAddCmd(nameItem, paraVal);
        }
        return true;
    }

    /// <summary>
    /// 初始化命令对象,分析sql语句,返回参数占位名字数组,如果没有参数,返回空数组.
    /// </summary>
    /// <param name="sql"></param>
    /// <returns></returns>
    private string[] NewCmdAndParseParaNames(string sql)
    {
        this.CmdInstance(sql);
        // 匹配出参数名后得到参数集合,使用该集合匹配对象中的属性,找到则赋值否则忽略(参数命名:字母开头可包含数字和下划线)
        MatchCollection paraNames = Regex.Matches(sql, this.paraRege);
        string[] names = new string[paraNames.Count];
        for (int i = 0, len = names.Length; i < len; i++)
        {
            names[i] = paraNames[i].Value;
        }
        return names;
    }
    /// <summary>
    /// 初始化参数对象,然后添加到命令
    /// </summary>
    /// <param name="nameItem"></param>
    /// <param name="paraVal"></param>
    private void NewParaAddCmd(string nameItem, object paraVal)
    {
        DbParameter para = this.ParaInstance(nameItem, paraVal);
        this.cmd.Parameters.Add(para);
    }

    /// <summary>
    /// 初始化命令,添加参数.为存储过程
    /// <para>parasdict为入参(in),outparasdict为出参(out).无参数时传null</para>
    /// </summary>
    private void InItCmdProc(string proc, Dictionary<string, object> parasdict,
        Dictionary<string, int> outparasdict)
    {
        this.CmdInstance(proc);

        // 加入in参数
        if (parasdict != null && parasdict.Count > 0)
        {
            foreach (string key in parasdict.Keys)
            {
                DbParameter para = this.ParaInstance(key, parasdict[key]);
                this.cmd.Parameters.Add(para);
            }
        }
        // 加入输出参数
        if (outparasdict != null && outparasdict.Count > 0)
        {
            foreach (string key in outparasdict.Keys)
            {
                DbParameter outpara = this.OutParaInstance(key, outparasdict[key]);
                this.cmd.Parameters.Add(outpara);
            }
        }
    }

    #endregion

    #region 执行增删改查存储过程 [内部方法]
    // 实际干活的方法(1.结果集查询(2种类型结果) 2.非查询 3.标量查询 4.存储过程 )

    /// <summary>
    /// 执行查询,返回字典数组查询结果.
    /// <para>字典的键是表字段名字或别名,值是字段值</para>
    /// </summary>
    private Dictionary<string, object>[] Select()
    {
        try
        {
            this.OpenDB();
            var data = this.ExecReader();
            return data == null ? null : [.. data];
        }
        catch (Exception e)
        {
            this.message = e.ToString();
            return null;
        }
        finally
        {
            this.CloseDB();
        }
    }

    /// <summary>
    /// 执行查询,生成强类型对象集合
    /// <para>E类型是一个实体对象,字段或属性名字与表字段名字对应.C#默认构造函数和字段值</para>
    /// <para>如果对应字段的数据值是DBNULL,那么E的该字段/属性将设置C#默认值.</para>
    /// </summary>
    private E[] Select<E>() where E : new()
    {
        try
        {
            this.OpenDB();
            var data = this.ExecReader();
            if (data == null)
                return null;
            List<E> redatalist = [];
            // 数据行
            for (int rowIndex = 0, len = data.Length; rowIndex < len; rowIndex++)
            {
                var row = data[rowIndex];
                // 创建一个实例
                E tmp = new();
                // 循环当行数据行的所有字段
                foreach (string k in row.Keys)
                {
                    // 优先查成员
                    bool hasMember = DBMO.FieldScan<E>(k, tmp, (field) =>
                    {
                        field.SetValue(tmp, Convert.IsDBNull(row[k])
                             ? default : Convert.ChangeType(row[k], field.FieldType));
                    });
                    if (hasMember == true)
                        continue;
                    // 再属性
                    DBMO.PropScan<E>(k, tmp, (prop) =>
                    {
                        prop.SetValue(tmp, Convert.IsDBNull(row[k])
                                 ? default : Convert.ChangeType(row[k], prop.PropertyType));
                    });
                    // 成员和属性都没有,不设置,数据值丢弃
                }
                redatalist.Add(tmp);
            }
            return [.. redatalist];

        }
        catch (Exception e)
        {
            this.message = e.ToString();
            return null;
        }
        finally
        {
            this.CloseDB();
        }
    }
    /// <summary>
    /// 执行查询,结果集(类内部实际干活方法)
    /// </summary>
    private Dictionary<string, object>[] ExecReader()
    {
        using DbDataReader dr = this.cmd.ExecuteReader();
        if (!dr.HasRows)
            return null;
        List<Dictionary<string, object>> re = [];
        while (dr.Read())
        {
            Dictionary<string, object> tmp = [];
            for (int i = 0; i < dr.FieldCount; i++)
            {
                tmp.Add(dr.GetName(i), dr[i]);
            }
            re.Add(tmp);
        }
        return [.. re];
    }
    /// <summary>
    /// 执行非查询,返回受影响行数(类内部实际干活方法)
    /// </summary>
    private int SelectNon()
    {
        try
        {
            this.OpenDB();
            return this.cmd.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            this.message = e.ToString();
            return -999;
        }
        finally
        {
            this.CloseDB();
        }
    }

    /// <summary>
    /// 执行一个标量查询 如果异常或者值是dbNULL都返回 null;
    /// </summary>
    private object SelectScalar()
    {
        try
        {
            this.OpenDB();
            object re = this.cmd.ExecuteScalar();
            if (Convert.IsDBNull(re))
            {
                return null;
            }
            return re;
        }
        catch (Exception e)
        {
            this.message = e.ToString();
            return null;
        }
        finally
        {
            this.CloseDB();
        }
    }

    /// <summary>
    /// 参数outparasdict: 字典实例.方法成功执行之后,字典会包含输出参数键值对.以输出参数名字为键名
    /// </summary>
    private int Procedure(Dictionary<string, object> outparasdict)
    {
        try
        {
            this.OpenDB();
            // 存储过程加了这句
            this.cmd.CommandType = System.Data.CommandType.StoredProcedure;
            // 执行过程
            int re = this.cmd.ExecuteNonQuery();
            // 找出当前命令参数集合里方向为out的全部参数,设置到outparasdict字典
            for (int i = 0; i < this.cmd.Parameters.Count; i++)
            {
                var item = this.cmd.Parameters[i];
                if (item.Direction == System.Data.ParameterDirection.Output)
                {
                    // 字典给键赋值时,如果无这个键,会自动加入,不必判断是否有键.
                    outparasdict[item.ParameterName] = item.Value;
                }
            }
            return re;
        }
        catch (Exception e)
        {
            this.message = e.ToString();
            return -999;
        }
        finally
        {
            this.CloseDB();
        }
    }

    #endregion

    #region 其它辅助方法

    /// <summary>
    /// 扫描实体类的指定名字public字段,并且执行一个方法.没找到字段返回false,方法不执行.
    /// <para>name 字段/属性名字,不区分大小写</para>
    /// </summary>
    private static bool FieldScan<T>(string name, T entity, Action<FieldInfo> action)
    {
        FieldInfo field = entity.GetType().GetField(name,
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.IgnoreCase);
        if (field == null) return false;
        action(field);
        return true;
    }

    /// <summary>
    /// 扫描实体类的指定名字public属性,并且执行一个方法.没找到属性返回false,方法不执行.
    /// <para>name 属性名字,不区分大小写</para>
    /// </summary>
    private static bool PropScan<T>(string name, T entity, Action<PropertyInfo> action)
    {
        PropertyInfo prop = entity.GetType().GetProperty(name,
        BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.IgnoreCase);
        if (prop == null) return false;
        action(prop);
        return true;
    }

    /// <summary>
    /// 自动补全INSERT SQL语句
    /// 指定表名列名,省略VALUES(),方法将自动补全,然后返回完整INSERT SQL
    /// </summary>
    /// <returns></returns>
    private static string AutoCmptInsertSql(string insertSql, char prefixChar)
    {
        string[] colarr = DBMO.FindSqlFieldPart(insertSql);
        if (colarr == null) return null;
        StringBuilder sqlpart = new();
        foreach (var item in colarr)
        {
            sqlpart.Append($"{prefixChar}{item.Trim('[', ']')},");
        }
        return $"{insertSql} VALUES({sqlpart.ToString().TrimEnd(',')})";
    }

    /// <summary>
    /// 自动补全UPDATE SQL语句
    /// 指定表名列名,省略SET,方法将自动补全,然后返回完整UPDATE SQL
    /// </summary>
    /// <returns></returns>
    private static string AutoCmptUpdateSql(string updateSql, char prefixChar)
    {
        int sindex = updateSql.IndexOf('(');
        int eindex = updateSql.IndexOf(')');
        string[] colarr = DBMO.FindSqlFieldPart(updateSql);
        if (colarr == null) return null;
        StringBuilder sqlpart = new();
        foreach (var item in colarr)
        {
            sqlpart.Append($"{item}={prefixChar}{item.Trim('[', ']')},");
        }
        // 以括号为分界点,前面0-sindex部分是update table 中间是拼成的col=@col eindex-最后,是where部分
        return $"{updateSql[..sindex].Trim()} SET {sqlpart.ToString().TrimEnd(',')} {updateSql[(eindex + 1)..].Trim()}";
    }

    /// <summary>
    /// 找出insert,update语句的字段部分,返回一个数组.例如insert tab (f1,f2),返回[f1,f2]
    /// </summary>
    /// <param name="sqlStr"></param>
    /// <returns></returns>
    private static string[] FindSqlFieldPart(string sqlStr)
    {
        // 传入的update例子 UPDATE TABLE(COL1,COL2,COL3..) where
        // 传入的insert例子 INSERT INTO TABLE(COL1,COL2,COL3..)
        // 1.找出第一个和第二个圆括号的位置2.取出中间的字段名,去掉空白后,逗号分组即可
        int sindex = sqlStr.IndexOf('(');
        int eindex = sqlStr.IndexOf(')');
        if (sindex == -1 || eindex == -1)
        {
            dbLog.DBLog($"请检查update/insert语句是否缺少左右括号:{sqlStr}");
            return null;
        }
        string cols = sqlStr.Substring(sindex + 1, eindex - sindex - 1);
        // 去掉空白
        cols = Regex.Replace(cols, @"\s", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return cols.Split(',');
    }
    #endregion
}