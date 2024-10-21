using MySqlX.XDevAPI;
using Org.BouncyCastle.Crypto.Paddings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection.Metadata;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Lib;

/// <summary>
/// 使用formdata表单数据,执行http post请求
/// </summary>
public class HttpFormHelp
{
    private readonly HttpClient _httpClient = HttpHelp.Client;
    private readonly string _url;
    /// <summary>
    /// 边界符开始,用于每一个表单项数据开始: 右侧换行,--开头
    /// </summary>
    private readonly byte[] beginBoundary;
    /// <summary>
    /// 边界符结束,用于整个表单结尾: 两侧换行,两侧都有--
    /// </summary>
    private readonly byte[] endBoundary;
    /// <summary>
    /// 换一行
    /// </summary>
    private readonly byte[] newLine;
    /// <summary>
    /// formdata类型
    /// </summary>
    private readonly string contentType;
    /// <summary>
    /// 字节容器
    /// </summary>
    private readonly List<byte> bf;

    /// <summary>
    /// 建立一个formdata实例,准备发送post请求
    /// </summary>
    /// <param name="headers"></param>
    public HttpFormHelp(string url, Dictionary<string, string> headers = null)
    {
        // 添加额外的header数据
        if (headers != null)
            AddHeaders(headers);

        this._url = url;

        // 数据分段标识:使用guid
        string boundary = Guid.NewGuid().ToString().Replace("-", "");

        // 
        this.beginBoundary = Encoding.UTF8.GetBytes($"--{boundary}{Environment.NewLine}");
        this.endBoundary = Encoding.UTF8.GetBytes($"{Environment.NewLine}--{boundary}--{Environment.NewLine}");
        this.contentType = $"multipart/form-data;charset=utf-8;boundary={boundary}";
        this.newLine = Encoding.UTF8.GetBytes(Environment.NewLine);
        this.bf = [];
    }

    /// <summary>
    /// 发送请求,返回字符串结果
    /// </summary>
    /// <returns></returns>
    public async Task<string> Send()
    {
        // 加入结尾--所有表单项添加完成后,加入结尾标志
        this.bf.AddRange(this.endBoundary);
        //
        HttpContent content = new ByteArrayContent([.. this.bf]);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(this.contentType);
        // 发送
        using HttpResponseMessage responseMessage = await this._httpClient.PostAsync(this._url, content);
        byte[] resultBytes = await responseMessage.Content.ReadAsByteArrayAsync();
        return Encoding.UTF8.GetString(resultBytes);
    }

    /// <summary>
    /// 为FormData表单,添加一个文件数据项
    /// </summary>
    /// <param name="formName">键名</param>
    /// <param name="fileName">文件名</param>
    /// <param name="file">内容</param>
    /// <param name="fileMimeType">默认application/octet-stream</param>
    /// <returns></returns>
    public void AddFile(string formName, string fileName, byte[] file, string fileMimeType = null)
    {
        // 文件类型和描述字段
        string desc = $"Content-Disposition:form-data;name=\"{formName}\";filename=\"{fileName}\"";
        string contentType = $"Content-type:{fileMimeType ?? "application/octet-stream"}";

        this.AddBeginBoundary();
        // 加入描述,内容类型
        this.AddDescItem(desc);
        this.AddDescItem(contentType);
        // 加入内容 数据描述和数据体之间,要空一行
        this.AddDataItem(file);
    }
    /// <summary>
    /// 为FormData表单,添加一个文本数据项
    /// </summary>
    /// <param name="formName">键名</param>
    /// <param name="fileName">文件名</param>
    /// <param name="file">文件内容</param>
    /// <param name="txtMimeType">默认:text/plain</param>
    /// <returns></returns>
    public void AddText(string formName, string text, string txtMimeType = null)
    {
        // 文本类型和描述字段
        string desc = $"Content-Disposition:form-data;name=\"{formName}\"";
        string contentType = $"Content-type:{txtMimeType ?? "text/plain"}";

        this.AddBeginBoundary();
        // 加入描述,内容类型
        this.AddDescItem(desc);
        this.AddDescItem(contentType);
        // 加入内容 数据描述和数据体之间,要空一行
        this.AddDataItem(Encoding.UTF8.GetBytes(text));
    }

    /// <summary>
    /// 为数据项加入开始分界符
    /// </summary>
    private void AddBeginBoundary()
    {
        // 继续加入数据时,有换行,也就是2个数据项之间,要有换行,否则就视为上个数据的继续.
        if (this.bf.Count > 0)
        {
            this.bf.AddRange(this.newLine);
        }
        // 加入开始边界
        this.bf.AddRange(this.beginBoundary);
    }

    /// <summary>
    /// 加入数据描述,数据mime类型
    /// </summary>
    /// <param name="item"></param>
    private void AddDescItem(string item)
    {
        this.bf.AddRange(Encoding.UTF8.GetBytes(item));
        this.bf.AddRange(this.newLine);
    }

    /// <summary>
    /// 加入数据体
    /// </summary>
    /// <param name="item"></param>
    private void AddDataItem(byte[] item)
    {
        // 数据描述和数据体之间,要空一行
        this.bf.AddRange(this.newLine);
        this.bf.AddRange(item);
        this.bf.AddRange(this.newLine);
    }

    private void AddHeaders(Dictionary<string, string> headers)
    {
        if (headers != default)
        {
            foreach (KeyValuePair<string, string> header in headers)
            {
                this._httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }
    }
}
