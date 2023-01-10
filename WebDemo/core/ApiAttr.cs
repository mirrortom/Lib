using Microsoft.AspNetCore.Http;
using System;

namespace WebDemo;

#region 功能特性
// 特性贴在webapi的方法上,AUTH特性也可以贴在类上
internal class WebApiBaseAttribute : Attribute
{
    /// <summary>
    /// 接口功能描述
    /// </summary>
    public string Desc { get; set; }
    /// <summary>
    /// 接口id(为每一个接口分配一个整数ID,用于权限判断)
    /// </summary>
    public int Id { get; set; }
}
internal class AUTHAttribute : WebApiBaseAttribute
{
    /// <summary>
    /// <para>判断解析token,检查登录者信息</para>
    /// <para>参考: string token = context.Request.Headers["Auth"].ToString();</para>
    /// <para>继承此特性,实现本方法,贴在webapi上</para>
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    internal virtual bool Authenticate(HttpContext context, ApiBase wabapi) => true;
}
internal class HTTPPOSTAttribute : WebApiBaseAttribute
{

}
internal class HTTPGETAttribute : WebApiBaseAttribute
{

}
#endregion

/// <summary>
/// 请求者信息,据需求修改此类
/// </summary>
internal class User
{
    public string Id { get; set; }
    public string Name { get; set; }
}
