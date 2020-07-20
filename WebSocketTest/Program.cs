using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketTest
{
    class Program
    {
        static void Main(string[] args)
        {

            // kestrel服务器配置文件载入
            IConfiguration kestrelCfg = new ConfigurationBuilder()
                .AddJsonFile("kestrel.json")
                .Build();

            // websocket配置文件
            var webSocketCfg = new WebSocketOptions()
            {
                // 向客户端发送“ping”帧的频率,以确保代理保持连接处于打开状态。.默认值为 2 分钟.
                KeepAliveInterval = TimeSpan.FromSeconds(120),
                // 用于接收数据的缓冲区的大小.高级用户可能需要对其进行更改,以便根据数据大小调整性能.默认值为4 KB.
                ReceiveBufferSize = 4 * 1024
            };

            // 开机运行
            // 实例化主机,载入配置项
            IWebHost webhost = new WebHostBuilder()
                .UseConfiguration(kestrelCfg)
                .UseKestrel()
                .Configure(app => app
                    .UseWebSockets(webSocketCfg)
                    .Use(websocketProcess)
                )
                .Build();
            //
            webhost.Run();
        }
        // 用于websocket处理的中间件
        private static RequestDelegate websocketProcess(RequestDelegate next)
        {
            async Task handler(HttpContext context)
            {
                // 这个ws是个区别标志,请求路径 ws开头的是websocket请求
                if (context.Request.Path == "/ws")
                {
                    // 是否为真正的websocket请求
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        if (t == null)
                        {
                            t = new System.Timers.Timer
                            {
                                Interval = 2000
                            };
                            t.Elapsed += T_Elapsed;
                            t.Start();
                        }
                        // 实例化
                        WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        Console.WriteLine("收到连接...,客户端Origin:" + context.Request.Headers["Origin"]);
                        // 消息处理
                        await Echo(context, webSocket);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }
                else
                {
                    // 传到下一个在中间(如果是非websocket请求,还要走其它一般请求的处理程序)
                    await next(context);
                }
            }
            return handler;
        }
        // websocket消息处理,阻塞
        private static async Task Echo(HttpContext context, WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {

                // 回应客户端消息
                //await webSocket.SendAsync(re, result.MessageType, result.EndOfMessage, CancellationToken.None);

                // 监听客户端消息,循环在这里等待了
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            // 客户端发起关闭连接时,执行
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }

        // 定时给每一个连接发报时
        private static void T_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            foreach (var k in connDict.Keys)
            {
                WebSocket webSocket = connDict[k];
                byte[] rebuffer = System.Text.Encoding.UTF8.GetBytes("服务器报时: " + DateTimeOffset.Now.ToString());
                ArraySegment<byte> re = new ArraySegment<byte>(rebuffer, 0, rebuffer.Length);

                webSocket.SendAsync(re, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
        private static System.Timers.Timer t;
        private static Dictionary<int, WebSocket> connDict;
    }
}
