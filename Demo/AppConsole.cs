namespace Demo;

/// <summary>
/// 控制台app模板 采用控制台界面的app的一种设计套路
/// CmdLoad() 装载入可用的命令
/// public Run() 程序开启方法,由shell调用.
/// Init() 程序初始化,比如加载配置,设定默认值
/// ExitAction() 退出程序
/// --命令--
///     命令: 就是提供的功能,类似于图形界面的菜单,web应用的url.
///     对象: 每个命令对象放在字典里,命令名字是键,值是2元组,含有:命令解释和执行方法
///     方法: 命令方法名字以Action结尾,有一个参数,无返回值.一般使用控制台程序时,输入一个命令,然后空格再加参数.
///     单线: 原则上每次运作周期只有一个命令执行.必须在一个命令执行完成后才可执行下一个.
/// </summary>
public class AppConsole
{
    /// <summary>
    /// 命令
    /// </summary>
    private Dictionary<string, (string desc, Action<object[]> method)> Cmds;

    /// <summary>
    /// 程序已经结束 长时间运行的服务程序可能有while循环,这个用于结束循环的标志
    /// </summary>
    private bool IsCancel;


    /// <summary>
    /// 应用程序名字
    /// </summary>
    private string AppName;

    public AppConsole()
    {
        this.Init();
        this.CmdLoad();
    }

    /// <summary>
    /// 开启程序
    /// </summary>
    public virtual void Run()
    {
        try
        {
            // 开始运行,打印程序信息和菜单
            Print($"[ {this.AppName} ]程序已经启动,欢迎使用!");
            this.ShowMenusAction();

            // 程序执行命令功能的主循环
            while (!this.IsCancel)
            {
                // 用程序名字做为shell提示符
                Console.Write($"<{this.AppName}>");
                this.CmdExec(Read());
            }
        }
        catch (Exception e)
        {
            Print($"[ {this.AppName} ]程序运行时发生异常,请重启! [ {e.Message} ]");
        }
    }

    /// <summary>
    /// 命令路由 查找和执行
    /// </summary>
    protected virtual void CmdExec(string input)
    {
        // 无输入,忽略
        if (string.IsNullOrWhiteSpace(input))
            return;
        // 命令错误
        string[] args = input.Trim().Split(' ');
        string cmd = args[0];
        if (!this.Cmds.ContainsKey(cmd))
        {
            Print("无效的命令!");
            return;
        }
        // 执行 传递的参数是除了命令名字外的输入
        this.Cmds[cmd].method(args[1..]);
    }

    /// <summary>
    /// 加载命令
    /// </summary>
    protected virtual void CmdLoad()
    {
        this.Cmds = new()
        {
            { "ls",("显示命令",this.ShowMenusAction) },
            { "exit",("退出程序",this.ExitAction)},
            { "cls",("清除屏幕",this.ClearAction)}
        };
    }

    /// <summary>
    /// 初始化,配置设定等等
    /// </summary>
    protected virtual void Init()
    {
        this.IsCancel = false;
        this.AppName = nameof(AppConsole);
    }

    #region Actions

    /// <summary>
    /// 显示命令列表
    /// </summary>
    /// <param name="args"></param>
    protected virtual void ShowMenusAction(params object[] args)
    {
        foreach (string key in this.Cmds.Keys)
        {
            Print($"{key}\t{this.Cmds[key].desc}");
        }
    }

    /// <summary>
    /// 退出程序 由exit命令调用
    /// </summary>
    /// <param name="args"></param>
    protected virtual void ExitAction(params object[] args)
    {
        this.IsCancel = true;
        Print($"[ {this.AppName} ]已关闭,感谢使用,bye bye!");
    }

    /// <summary>
    /// 清除内容
    /// </summary>
    /// <param name="args"></param>
    protected virtual void ClearAction(params object[] args)
    {
        Console.Clear();
    }
    #endregion

    #region Tools

    /// <summary>
    /// Console.WriteLine 让方法名字短点
    /// </summary>
    /// <param name="msg"></param>
    private void Print(string msg)
    {
        Console.WriteLine(msg);
    }
    /// <summary>
    /// Console.ReadLine 让方法名字短点
    /// </summary>
    private string Read()
    {
        return Console.ReadLine();
    }

    #endregion
}
