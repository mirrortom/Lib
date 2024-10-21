using System.Net;
using System.Net.Sockets;
using System.Text;

using static Demo.tcp.Tool;
namespace Demo.tcp;

/// <summary>
/// tcp服务端管理
/// </summary>
public class TcpService
{
    /// <summary>
    /// 命令
    /// </summary>
    private Dictionary<string, (string desc, Action<object[]> method)> Cmds;

    /// <summary>
    /// 默认服务器IP终结点
    /// </summary>
    private IPAddress iPAddress = IPAddress.Any;
    private int port = 5_0000;

    /// <summary>
    /// socket对象
    /// </summary>
    Socket socket;

    /// <summary>
    /// 最大客户端连接数(The maximum length of the pending connections queue)
    /// </summary>
    private int MaxPending = 100;

    /// <summary>
    /// 连接对象列表
    /// </summary>
    List<TcpWorker> TcpWorkers;

    /// <summary>
    /// 服务程序是否已取消
    /// </summary>
    private bool IsCancel;

    /// <summary>
    /// 应用程序名字
    /// </summary>
    private string AppName = "Socket tcp service demo";

    public TcpService()
    {
        this.Init();
        this.CmdLoad();
    }

    /// <summary>
    /// 启动管理器 在控制台环境中,可以操作一个客户端
    /// </summary>
    /// <returns></returns>
    public void Run()
    {
        try
        {
            this.StartListen();

            // 开始运行,打印程序信息和菜单
            Print($"[ {this.AppName} ]服务端演示程序已经启动,欢迎使用!");
            this.ShowMenusAction();

            // 程序执行命令功能的主循环
            while (!this.IsCancel)
            {
                // 用程序名字做为shell提示符
                Console.Write($"<{this.AppName}>");
                this.CmdExec(Console.ReadLine());
            }
        }
        catch (Exception e)
        {
            Print($"[ {this.AppName} ]程序运行时发生异常,请重启! [ {e.Message} ]");
        }
    }

    private void Init()
    {
        this.TcpWorkers = new List<TcpWorker>();
        this.IsCancel = false;
    }

    private Task StartListen()
    {
        void listenThread()
        {
            // 监听传入的连接
            try
            {
                this.socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                this.socket.Bind(new IPEndPoint(this.iPAddress, this.port));
                this.socket.Listen(this.MaxPending);
                while (!this.IsCancel)
                {
                    if (this.TcpWorkerCount() >= this.MaxPending)
                    {
                        Print($"连接已满{this.ClientCountInfo()},不再接受新客户端!");
                        Thread.Sleep(1000);
                        continue;
                    }
                    Socket sockClient = this.socket.Accept();
                    Print($"检测到新连接: {sockClient.RemoteEndPoint}");
                    TcpWorker work = new(sockClient);
                    this.TcpWorkers.Add(work);

                }
            }
            catch (Exception e)
            {
                Print($"[{this.iPAddress}:{this.port}] 请求监视程序已经取消,引发异常: {e.Message}");
            }
        }
        return Task.Factory.StartNew(listenThread);
    }

    /// <summary>
    /// 从连接列表里删除已关闭的的连接
    /// </summary>
    private void RemoveWorker()
    {
        this.TcpWorkers = this.TcpWorkers.FindAll(item => item.IsClosed == false);
    }

    /// <summary>
    /// 客户端连接数信息
    /// </summary>
    /// <returns></returns>
    private string ClientCountInfo()
    {
        return $"{this.TcpWorkerCount()}/{this.TcpWorkers.Count}";
    }

    /// <summary>
    /// 获取连接对象列表中正常连接的个数
    /// </summary>
    /// <returns></returns>
    private int TcpWorkerCount()
    {
        return this.TcpWorkers.FindAll(item => item.IsClosed == false).Count;
    }

    #region cmd

    /// <summary>
    /// 命令路由 查找和执行
    /// </summary>
    private void CmdExec(string input)
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
    /// 装载命令:命令是键,对应方法是值
    /// </summary>
    private void CmdLoad()
    {
        this.Cmds = new() {
            {"menu",("命令列表",this.ShowMenusAction) },
            {"info",("服务器信息",this.ServiceInfoAction)},
            {"ls",("列出所有客户端",this.ListClientsAction)},
            {"chg",("切换到客户端[:chg 编号]",this.ChgWorkerAction)},
            {"rm",("关闭客户端[:rm 编号|closed已关闭的]",this.RemoveWorkerAction)},
            {"exit",("退出程序",this.ExitAction)},
            {"cls",("清除内容",this.ClearAction)}
        };
    }

    #endregion

    #region Actions

    /// <summary>
    /// 显示命令列表
    /// </summary>
    /// <param name="args"></param>
    private void ShowMenusAction(params object[] args)
    {
        foreach (string key in this.Cmds.Keys)
        {
            Print($"{key}\t{this.Cmds[key].desc}");
        }
    }

    /// <summary>
    /// 服务器信息
    /// </summary>
    /// <param name="args"></param>
    private void ServiceInfoAction(params object[] args)
    {
        StringBuilder info = new();
        info.AppendLine($"地址:\t{this.socket.LocalEndPoint}");
        info.AppendLine($"最大连接数:\t{this.MaxPending}");
        info.Append($"客户端已连接数/总数:\t{this.ClientCountInfo()}");
        Print(info.ToString());
    }

    /// <summary>
    /// 显示所有客户端
    /// </summary>
    /// <param name="args"></param>
    private void ListClientsAction(params object[] args)
    {
        if (this.TcpWorkers.Count == 0)
        {
            Print("没有连接的客户端!");
            return;
        }
        Print($"[编号]\t地址\t状态\t时长S");
        for (int i = 0; i < this.TcpWorkers.Count; i++)
        {
            var item = this.TcpWorkers[i];
            string status = item.IsClosed ? "关闭" : "已连接";
            Print($"[{i}]\t{item.Address}\t{status}\t{item.ConnectedTime}");
        }
    }

    /// <summary>
    /// 停止监听 退出服务
    /// </summary>
    private void ExitAction(params object[] args)
    {
        // 服务端主动关闭所有连接
        foreach (var item in this.TcpWorkers)
        {
            item.Close();
        }
        this.TcpWorkers = null;

        // 关闭监听
        this.socket.Close();
        this.IsCancel = true;
        Print($"[ {this.AppName} ]已关闭,感谢使用,bye bye!");
    }

    /// <summary>
    /// 切换到一个已有的客户端连接.推送消息
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    private void ChgWorkerAction(params object[] args)
    {
        if (args.Length == 0)
        {
            Print("输入必须参数,客户端编号");
            return;
        }
        string para = args[0].ToString();
        if (!int.TryParse(para, out int index) || index < 0 || index >= this.TcpWorkers.Count)
        {
            Print("客户端编号无效!");
            return;
        }
        if (!this.TcpWorkers[index].IsClosed)
        {
            Print($"[ {this.TcpWorkers[index].Address} ]已切换这个客户端!");
            Print($"请输入推送内容... (:q *冒号q*退出该客户端)");
            while (true)
            {
                string msg = Read().Trim();
                if (msg.Equals(":q", StringComparison.CurrentCultureIgnoreCase))
                    break;
                // 可能会写入失败,因为连接已经关闭
                if (this.TcpWorkers[index].Write(msg))
                    continue;
                break;
            }
            return;
        }
        Print($"客户端 [ {index} ] 已经失去连接!");
    }

    /// <summary>
    /// 删除指定客户端或者已关闭客户端
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    private void RemoveWorkerAction(params object[] args)
    {
        if (args.Length == 0)
        {
            Print("输入必须参数,客户端编号或者closed");
            return;
        }
        string para = args[0].ToString();
        if (para == "closed")
        {
            this.RemoveWorker();
            Print("已删除关闭的客户端!");
            return;
        }
        if (!int.TryParse(para, out int index) || index < 0 || index >= this.TcpWorkers.Count)
        {
            Print("客户端编号无效!");
            return;
        }
        if (!this.TcpWorkers[index].IsClosed)
            this.TcpWorkers[index].Close();
        this.TcpWorkers.RemoveAt(index);
        Print($"客户端 [ {index} ] 已删除!");
    }

    /// <summary>
    /// 清除内容
    /// </summary>
    /// <param name="args"></param>
    private void ClearAction(params object[] args)
    {
        Console.Clear();
    }
    #endregion
}
