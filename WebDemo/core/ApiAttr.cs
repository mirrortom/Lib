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
internal class HTTPPOSTAttribute : WebApiBaseAttribute
{

}
internal class HTTPGETAttribute : WebApiBaseAttribute
{

}

/// <summary>
/// 验证,用于API类或方法
/// </summary>
internal class AUTHBaseAttribute : WebApiBaseAttribute
{
    /// <summary>
    /// <para>判断解析token,检查登录者信息,按需重写本方法</para>
    /// </summary>
    /// <returns></returns>
    internal virtual bool Authenticate(HttpContext context) => true;
}

#endregion