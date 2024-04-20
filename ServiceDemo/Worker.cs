using Lib;
using Lib.dbm;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceDemo;

/// <summary>
/// 定时器服务
/// </summary>
internal class Worker : IHostedService
{
    /// <summary>
    /// 定时器
    /// </summary>
    private readonly System.Timers.Timer Timer1;
    /// <summary>
    /// 定时器执行任务间隔时间(毫秒)
    /// </summary>
    private readonly double interval = 5000;
    /// <summary>
    /// 任务正在执行中为true
    /// </summary>
    private bool IsWorking;

    /// <summary>
    /// 建立服务,建立定时器
    /// </summary>
    public Worker()
    {
        // init
        Timer1 = new System.Timers.Timer(interval);
        Timer1.Elapsed += Timer1_Elapsed;
    }

    /// <summary>
    /// 定时器方法,执行具体任务
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Timer1_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        // 对于耗时的任务,可能会超过一个时间间隔还没有执行完.具体看情况处理.
        // 任务开始时,暂停定时器.
        Timer1.Stop();

        // 任务
        //BackupDB();
        TestLog();

        // 任务结束后,开启定时器
        Timer1.Start();
    }
    //  任务示例 打印一个日志
    private void TestLog()
    {
        NLogHelp.Log($"Time: {DateTime.Now}");
    }

    // 任务示例 执行存储过程,定时备份数据库
    private void BackupDB()
    {
        NLogHelp.SrvLog("开始...");
        try
        {
            SQLServer db = new();
            // 备份数据库的sql是一个proc,名字用全名,"库.架构.proc名"
            string proc = "demo.dbo.demo_fullbak";
            Dictionary<string, int> outputTypeDict = new()
            {
                {"@result",(int)System.Data.SqlDbType.Int}
            };
            int re = db.ExecuteProcedure(proc, null, outputTypeDict, out Dictionary<string, object> output);
            NLogHelp.SrvLog($"结果: {re} , @result:{output["@result"]}");
        }
        catch (Exception e)
        {
            NLogHelp.SrvLog(e.Message);
            NLogHelp.SrvLog(e.ToString());
        }
    }
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Timer1.Start();
#if DEBUG
        Console.WriteLine("服务开启,计时器开启!");
#endif
        NLogHelp.SrvLog("服务开启,计时器开启!");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Timer1.Close();
#if DEBUG
        Console.WriteLine("服务/计时器关闭!");
#endif
        NLogHelp.SrvLog("服务/计时器关闭!");
        return Task.CompletedTask;
    }
}
