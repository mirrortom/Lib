using System.Reflection;

namespace Demo.ConsoleApp;

/// <summary>
/// 控制台app模板 采用控制台界面的app的一种设计套路.使用时可继承或套用此类.
/// </summary>
public class ConsoleMain
{
    /// <summary>
    /// 命令
    /// </summary>
    private SortedDictionary<string, IConsoleApp> Cmds;

    /// <summary>
    /// 程序已经结束 长时间运行的服务程序可能有while循环,这个用于结束循环的标志
    /// </summary>
    private bool IsCancel;

    /// <summary>
    /// 应用程序名字
    /// </summary>
    private string AppName;

    public ConsoleMain()
    {
        // 初始化,配置设定等等
        IsCancel = false;
        AppName = nameof(ConsoleMain);
        // 加载应用命令
        this.Cmds = new();
        LoadAppCommands();
    }

    /// <summary>
    /// 开启程序
    /// </summary>
    public virtual void Run()
    {
        try
        {
            // 开始运行,打印程序信息和菜单
            Print($"[ {AppName} ]程序已经启动,欢迎使用!");
            ShowMenusAction();

            // 程序执行命令功能的主循环
            while (!IsCancel)
            {
                // 用程序名字做为shell提示符
                Console.Write($"<{AppName}>");
                ExecCmd(Read());
            }
        }
        catch (Exception e)
        {
            Print($"[ {AppName} ]程序运行时发生异常,请重启! [ {e.Message} ]");
        }
    }

    /// <summary>
    /// 命令路由 查找和执行
    /// </summary>
    protected virtual void ExecCmd(string input)
    {
        // 无输入
        if (string.IsNullOrWhiteSpace(input))
            return;
        // 命令和参数分开
        string[] args = input.Trim().Split(' ');
        string cmd = args[0];
        // 执行命令
        // 系统命令
        switch (cmd)
        {
            case "show":
                ShowMenusAction(args);
                return;

            case "exit":
                ExitAction(args);
                return;

            case "cls":
                ClearAction(args);
                return;

            default:
                break;
        }
        // 应用命令
        if (!Cmds.ContainsKey(cmd))
        {
            Print("无效的命令!");
            return;
        }
        // 执行 传递的参数是除了命令名字外的输入
        Cmds[cmd].Run(args[1..]);
    }

    /// <summary>
    /// 加载应用
    /// </summary>
    protected virtual void LoadAppCommands()
    {
        // 找出main函数所在程序集中所有实现了IConsoleApp的类
        Type[] types = Assembly.GetEntryAssembly().GetExportedTypes();

        foreach (Type t in types)
        {
            if (t.GetInterface(nameof(IConsoleApp)) != null)
            {
                IConsoleApp obj = Activator.CreateInstance(t) as IConsoleApp;
                if (Cmds.ContainsKey(obj.Name))
                {
                    throw new Exception($"服务启动失败,命令 ({obj.Name}) 重复!");
                }
                Cmds.Add(obj.Name, obj);
            }
        }
    }

    #region 系统内置命令

    /// <summary>
    /// 显示命令列表
    /// </summary>
    /// <param name="args"></param>
    protected virtual void ShowMenusAction(params object[] args)
    {
        Print("系统命令 :");
        Print("show : 显示命令列表.");
        Print("exit : 退出.");
        Print("cls : 清屏.");
        Print("应用命令 :");
        foreach (string key in Cmds.Keys)
        {
            Print($"{key}\t{Cmds[key].Desc}");
        }
    }

    /// <summary>
    /// 退出程序 由exit命令调用
    /// </summary>
    /// <param name="args"></param>
    protected virtual void ExitAction(params object[] args)
    {
        IsCancel = true;
        Print($"[ {AppName} ]已关闭,感谢使用,bye bye!");
    }

    /// <summary>
    /// 清除内容
    /// </summary>
    /// <param name="args"></param>
    protected virtual void ClearAction(params object[] args)
    {
        Console.Clear();
    }

    #endregion 系统内置命令

    #region Tools

    /// <summary>
    /// Console.WriteLine 让方法名字短点
    /// </summary>
    /// <param name="msg"></param>
    protected void Print(string msg)
    {
        Console.WriteLine(msg);
    }

    /// <summary>
    /// Console.ReadLine 让方法名字短点
    /// </summary>
    protected string Read()
    {
        return Console.ReadLine();
    }

    #endregion Tools
}