using MySqlX.XDevAPI;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Lib;

// 文档:https://docs.microsoft.com/zh-cn/dotnet/api/system.net.http.httpclient?view=net-6.0
public class HttpHelp
{
    private static HttpClient client;
    /// <summary>
    /// 实例,首次请求时会实例化httpclient对象.
    /// 在需要设置client属性时,才会使用此属性
    /// </summary>
    public static HttpClient Client
    {
        get
        {
            client ??= new();
            return client;
        }
    }

    /// <summary>
    /// 取消当前httpclient的所有请求,然后释放这个实例.
    /// 当HttpClient被使用过之后,再修改它们的属性会抛出错误.如果要修改,先释放对象,再建立新的(调用请求方法时会自动建立)
    /// </summary>
    public static void Close()
    {
        Client.CancelPendingRequests();
        Client.Dispose();
        client = null;
    }

    /// <summary>
    /// get请求获取字符串类型结果.例如html文档
    /// </summary>
    /// <param name="url">地址</param>
    /// <param name="headers">headers参数键值对</param>
    /// <returns></returns>
    public static async Task<string> GetString(string url, Dictionary<string, string> headers = null)
    {
        AddHeaders(headers);
        return await Client.GetStringAsync(url);
    }
    /// <summary>
    /// get请求,返回一个字节数组
    /// </summary>
    /// <param name="url"></param>
    /// <param name="headers"></param>
    /// <returns></returns>
    public static async Task<byte[]> GetBytes(string url, Dictionary<string, string> headers = null)
    {
        AddHeaders(headers);
        return await Client.GetByteArrayAsync(url);
    }

    /// <summary>
    /// post请求,文本参数(例如json格式文本).返回文本结果
    /// </summary>
    /// <param name="url"></param>
    /// <param name="json"></param>
    /// <param name="headers"></param>
    /// <returns></returns>
    public static async Task<string> PostTextString(string url, string json, Dictionary<string, string> headers = null)
    {
        AddHeaders(headers);
        HttpContent content = new StringContent(json);
        return await PostStringResult(url, content);
    }


    /// <summary>
    /// 发送post请求,返回string结果
    /// </summary>
    /// <param name="url"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    private static async Task<string> PostStringResult(string url, HttpContent content)
    {
        using HttpResponseMessage responseMessage = await Client.PostAsync(url, content);
        byte[] resultBytes = await responseMessage.Content.ReadAsByteArrayAsync();
        return Encoding.UTF8.GetString(resultBytes);
    }

    private static void AddHeaders(Dictionary<string, string> headers)
    {
        if (headers != default)
        {
            foreach (KeyValuePair<string, string> header in headers)
            {
                Client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }
    }
}
