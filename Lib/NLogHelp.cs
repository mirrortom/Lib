using NLog;
using NLog.Config;
using NLog.Targets;

namespace Lib;

/// <summary>
/// Nlog 记录日志,未经过大量实践
/// </summary>
public static class NLogHelp
{
    // 3种目录的记录器
    // 1.输出到文件(用于数据库,文件夹 DBLog)
    private static Logger logDB;
    // 2.输出到文件(用于服务,文件夹 SrvLog)
    private static Logger logSrv;
    // 3.输出到文件(用于App,Web,文件夹 AppLog)
    private static Logger log;
    // 4.输出到控制台(用于测试调试)
    private static Logger debug;

    // 日志格式说明-用于文件
    // ${date} 日期,例: 2020/10/03 12:10:01.749
    // ${stacktrace} 调用层次堆栈(不算本类,2层):调用者的类和方法名,例:Test.Program.Main=>BB.Run
    // ${newline} 换行,跨平台的
    // ${message}日志内容
    // 更多layout字段: https://nlog-project.org/config/?tab=layout-renderers
    private static readonly string contLayout = @"${date}|${stacktrace:topFrames=3:skipFrames =1}${newline}${message}${newline}${newline}";

    // 日志格式-用于控制台
    private static readonly string consoleLayout = @"${time}|${message:withexception=true}";

    // 日志目录文件名,模式
    // ${basedir} 当前应用程序运行根目录.可在init中传入其它目录地址.
    // ${date:format=yyyy}每年一个目录
    // ${date:format=MM}每月一个目录
    // 如果日志多,可以继续分下级目录.
    // ${shortdate}.log 以每天年月日做文件名字,2020-10-04.log
    private static readonly string fileNameTpl = @"${basedir}/${logger}/${date:format=yyyy}/${date:format=MM}/${shortdate}.log";

    // 日志大小2M上限后,新开文件
    private static readonly long maxFileSize = 2 * 1024 * 1024;

    /// <summary>
    /// 日志初始化,使用前需要调用该方法.
    /// </summary>
    /// <param name="LogRootPath">默认是程序运行目录.(设定新的必须是绝对路径)(例 e:/logs 或者 /home/log)</param>
    public static void Init(string LogRootPath = null)
    {
        // 1.根目录检查
        string fileFullPath = fileNameTpl;
        if (!string.IsNullOrWhiteSpace(LogRootPath))
        {
            fileFullPath = fileNameTpl.Replace("${basedir}", LogRootPath);
        }

        // 2.初始化3个文件型日志记录器,区别只在于目录设置不同
        var cfg = new LoggingConfiguration();
        string[] logType = { "SrvLog", "DBLog", "AppLog", "Console" };
        for (int i = 0; i < 3; i++)
        {
            var target = new FileTarget
            {
                Name = logType[i],
                Layout = contLayout,
                FileName = fileFullPath,
                ArchiveAboveSize = maxFileSize,
                // 这个要开启,否则性能极差
                // https://github.com/NLog/NLog/wiki/File-target
                KeepFileOpen = true,
                ConcurrentWrites = false
            };
            // 加入到配置器
            cfg.AddTarget(target);
            // 级别(Trace,Debug,Info,Warn,Error,Fatal),这里都用Info
            cfg.AddRule(LogLevel.Info, LogLevel.Info, target);
        }
        // 2.1控制台记录器
        var consoleTarget = new ConsoleTarget(logType[3]);
        consoleTarget.Layout = consoleLayout;
        cfg.AddTarget(consoleTarget);
        cfg.AddRule(LogLevel.Debug, LogLevel.Debug, consoleTarget);
        // 3.加载配置,生成
        LogManager.Configuration = cfg;
        // 绑定变量
        logSrv = LogManager.GetLogger(logType[0]);
        logDB = LogManager.GetLogger(logType[1]);
        log = LogManager.GetLogger(logType[2]);
        debug = LogManager.GetLogger(logType[3]);
    }

    public static void SrvLog(string msg)
    {
        logSrv.Info(msg);
    }
    public static void DBLog(string msg)
    {
        logDB.Info(msg);
    }
    public static void Log(string msg)
    {
        log.Info(msg);
    }
    public static void Debug(string msg)
    {
        debug.Debug(msg);
    }
}
