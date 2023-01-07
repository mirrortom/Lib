using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Lib;

// 文档:https://docs.microsoft.com/zh-cn/dotnet/api/system.net.http.httpclient?view=net-6.0
public class HttpHelp
{
    // HttpClient 旨在每个应用程序实例化一次而不是每次使用
    // HttpClient 用于在应用程序的整个生存期内实例化一次并重复使用. 实例化每个请求的 HttpClient 类将耗尽重负载下可用的插槽数. 这将导致 SocketException 错误.
    private static readonly HttpClient client = new();

    /// <summary>
    /// get请求获取字符串类型结果.例如html文档
    /// </summary>
    /// <param name="url">地址</param>
    /// <param name="headers">headers参数键值对</param>
    /// <param name="timeOutSecond">请求超时秒,(不传或者0时,默认100秒)</param>
    /// <returns></returns>
    public static async Task<string> GetString(string url, Dictionary<string, string> headers = null, int timeOutSecond = 0)
    {
        RequestInit(headers, timeOutSecond);
        return await client.GetStringAsync(url);
    }

    public static async Task<byte[]> GetBytes(string url, Dictionary<string, string> headers = null, int timeOutSecond = 0)
    {
        RequestInit(headers, timeOutSecond);
        return await client.GetByteArrayAsync(url);
    }

    private static void RequestInit(Dictionary<string, string> headers, int timeOutSecond)
    {
        if (headers != default)
        {
            foreach (KeyValuePair<string, string> header in headers)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }
        if (timeOutSecond > 0)
        {
            client.Timeout = TimeSpan.FromSeconds(timeOutSecond);
        }
    }
}
