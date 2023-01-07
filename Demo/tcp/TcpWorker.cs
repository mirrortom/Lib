using System.Net.Sockets;
using System.Text;
using System.Timers;

namespace Demo.tcp;

/// <summary>
/// 服务端
/// </summary>
internal class TcpWorker
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

    public TcpWorker(TcpClient client, uint maxTime = 0)
    {
        try
        {
            this.client = client;
            this.MaxTime = maxTime;
            this.IsClosed = false;
            // 连接时间计时
            this.Timer = new System.Timers.Timer(1000);
            this.Timer.Elapsed += Timer_ConnectedTime;
            this.Timer.Start();
            // 通信驻留线程
            this.WaitReadGuard();
        }
        catch (Exception e)
        {
            this.IsClosed = true;
            this.Timer?.Stop();
            Print($"建立tcp连接服务对象时发生异常: {e.Message}");
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
    /// 客户端地址 如果客户端程序退出或者关闭,返回-
    /// </summary>
    public string Address
    {
        get
        {
            return this.client.Connected ? this.client.Client.RemoteEndPoint.ToString() : "-";
        }
    }

    /// <summary>
    /// 等待读取来自客户端消息的驻留线程,每次收到消息后,自动回复一个消息(客户端连接持续时间)
    /// </summary>
    private void WaitReadGuard()
    {
        // 读取消息线程
        Task.Run(() =>
        {
            // 如果在循环中发生读写异常,会关闭连接,退出线程
            while (true)
            {
                var (msg, success) = this.Read();
                if (success == true)
                {
                    Print($"[ {this.Address} ] 客户端新消息: {msg}");
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
    private (string msg, bool success) Read()
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
            // 如果一直收到空内容,那么可能是客户端网络异常或它主动断开了连接
            if (lastIndex == 0)
            {
                throw new Exception("客户端关闭了连接!");
            }
            return (Encoding.UTF8.GetString(buffer, 0, lastIndex), true);
        }
        // 发生错误时,主动关闭连接
        catch (Exception e)
        {
            // 如果是服务端主动关闭(调用了this.Close())引起的异常,不需要再调用关闭
            if (!this.IsClosed)
                this.Close();
            Print($"读取客户端异常: {e.Message}");
            return (string.Empty, false);
        }
    }

    /// <summary>
    /// 发送内容
    /// 内容必须小于1024字节,10秒超时
    /// 写入异常时,返回false,连接关闭
    /// </summary>
    /// <param name="msg"></param>
    internal bool Write(string msg)
    {
        try
        {
            if (this.client.Connected == false)
            {
                throw new Exception("连接对象已经关闭!");
            }
            var stream = this.client.GetStream();
            stream.WriteTimeout = 10_000;
            byte[] buffer = Encoding.UTF8.GetBytes(msg);
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
            Print($"写入客户端异常: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 主动断开client连接
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
