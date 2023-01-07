using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;

namespace Demo.tcp;

/// <summary>
/// tcp客户端
/// </summary>
internal class TcpClientWorker
{
    private readonly TcpClient client;
    /// <summary>
    /// 最大连接时间,秒单位.过时主动关闭,0=不限制
    /// </summary>
    private readonly uint MaxTime;
    /// <summary>
    /// 建立连接到关闭时经过的秒数
    /// </summary>
    private uint connectedTime;
    private readonly System.Timers.Timer Timer;

    public TcpClientWorker(IPAddress iP, int port, uint maxTime = 0)
    {
        try
        {
            this.client = new();
            client.Connect(iP, port);
            this.IsClosed = false;
            this.MaxTime = maxTime;
            // 连接时间计时
            this.Timer = new System.Timers.Timer(1000);
            this.Timer.Elapsed += Timer_ConnectedTime;
            this.Timer.Start();
            // 等待远方消息线程
            this.WaitReadGuard();
        }
        catch (Exception e)
        {
            this.IsClosed = true;
            this.Timer?.Stop();
            Print($"建立tcp客户端对象时发生异常: {e.Message}");
        }
    }

    private void Timer_ConnectedTime(object? sender, ElapsedEventArgs e)
    {
        this.connectedTime++;
    }

    /// <summary>
    /// 连接已关闭为true
    /// </summary>
    /// <returns></returns>
    public bool IsClosed { get; private set; }

    /// <summary>
    /// 已经连接秒数
    /// </summary>
    public uint ConnectedTime { get { return this.connectedTime; } }

    /// <summary>
    /// 服务端地址 如果服务端程序退出或者关闭,返回-
    /// </summary>
    public string ServeAddress
    {
        get
        {
            return this.client.Connected ? this.client.Client.RemoteEndPoint.ToString() : "-";
        }
    }
    /// <summary>
    /// 客户端本地端点地址
    /// </summary>
    public string Address
    {
        get
        {
            return this.client.Connected ? this.client.Client.LocalEndPoint.ToString() : "-";
        }
    }
    /// <summary>
    /// 等待读取来自服务端推送消息的驻留线程
    /// </summary>
    private void WaitReadGuard()
    {
        // 读取消息线程
        Task.Run(() =>
        {
            // 如果在循环中读取消息异常,关闭连接,退出线程
            while (true)
            {
                var (msg, success) = this.Read();
                if (success == true)
                {
                    Print($"[ {this.Address} ] 服务端新消息: {msg}");
                    continue;
                }
                break;
            }
        });
    }

    /// <summary>
    /// 读取内容,内容必须小于1024字节,
    /// 读取异常时, success=false ,连接关闭
    /// </summary>
    /// <returns></returns>
    public (string receive, bool success) Read()
    {
        try
        {
            if (this.client.Connected == false)
            {
                throw new Exception("连接对象已经关闭!");
            }
            var stream = this.client.GetStream();
            // 一直等待消息,
            //stream.ReadTimeout = 10_000;
            byte[] buffer = new byte[1024];
            int lastIndex = stream.Read(buffer, 0, buffer.Length);
            // 如果网络异常或服务端主动关闭了连接,那么会一直收到0长度的消息
            if (lastIndex == 0)
            {
                throw new Exception("服务端关闭了连接!");
            }
            return (Encoding.UTF8.GetString(buffer, 0, lastIndex), true);
        }
        // 发生错误时,主动关闭连接
        catch (Exception e)
        {
            // 如果是客户端主动关闭(调用了this.Close())引起的异常,不需要再调用关闭
            if (!this.IsClosed)
                this.Close();
            Print($"接收服务端消息异常: {e.Message}");
            return (string.Empty, false);
        }
    }

    /// <summary>
    /// 发送内容
    /// 内容必须小于1024字节,10秒超时
    /// 写入异常时,返回false,连接关闭
    /// </summary>
    /// <param name="msg"></param>
    public bool Write(string send)
    {
        try
        {
            if (this.client.Connected == false)
            {
                throw new Exception("连接对象已经关闭!");
            }
            var stream = this.client.GetStream();
            stream.WriteTimeout = 10_000;
            byte[] buffer = Encoding.UTF8.GetBytes(send);
            if (buffer.Length > 1024)
            {
                Print("发送内容不能超过1024字节!");
                return false;
            }
            stream.Write(buffer, 0, buffer.Length);
            return true;
        }
        // 发生错误时,主动关闭连接
        catch (Exception e)
        {
            // 如果是服务端主动关闭(调用了this.Close())引起的异常,不需要再调用关闭
            if (!this.IsClosed)
                this.Close();
            Print($"发送消息异常: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 关闭连接
    /// </summary>
    public void Close()
    {
        this.IsClosed = true;
        this.Timer.Stop();
        // 主动关闭连接中的socket会引发异常 Read()Write()方法会捕获到异常
        this.client.Close();
    }

    /// <summary>
    /// Console.WriteLine
    /// </summary>
    /// <param name="msg"></param>
    private void Print(string msg)
    {
        Console.WriteLine(msg);
    }
}
