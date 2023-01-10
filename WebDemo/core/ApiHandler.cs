using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace WebDemo;

// 自定义中间件
internal class ApiHandler
{
    /// <summary>
    /// 分析url地址然后执行对应的类和方法.(中间件)
    /// </summary>
    /// <param name="next"></param>
    /// <returns></returns>
    internal static Task UrlMapMethodMW(HttpContext context)
    {
        // 程序集名(当前运行此代码的程序集)
        string assbName = Assembly.GetExecutingAssembly().GetName().Name;

        // 分解URL路径用于找类名和方法名
        string[] urlparts = context.Request.Path.Value.Split('/');

        // 如果路径不合规则,结束请求!合法路径 /api/user/info , /user/name
        // 如果开启了静态文件,url所在文件没找到时,也会进入这里
        if (urlparts.Length < 3)
        {
            return ApiHandler.ErrorEnd(context, 5001, $"Url is invalid! [{context.Request.Path.Value}]");
        }

        // 类名和方法名:类名全称是- 程序集名.[命名空间].类名Api(约定后缀)
        // 命名空间: url拆分后,0位是命名空间,如果只有两段,则没有命名空间(只有当前程序集的默认命名空间)
        // 后缀约定: 为了统一书写,一个成为api的类,结尾为Api.例如: UserApi
        string apiClass = "", apiMethod = "";
        if (urlparts.Length < 4)
        {
            apiClass = $"{assbName}.{urlparts[1]}Api";
            apiMethod = urlparts[2];
        }
        else
        {
            apiClass = $"{assbName}.{urlparts[1]}.{urlparts[2]}Api";
            apiMethod = urlparts[3];
        }

        // 到当前程序集中寻找这个类
        Type webapiT = Assembly.GetExecutingAssembly().GetType(apiClass, false, true);

        // 未找到该类,结束请求!
        if (webapiT == null)
        {
            return ApiHandler.ErrorEnd(context, 5002, $"Class not found! [{apiClass}]");
        }

        // 建立实例,再传入HttpContext对象
        // 继承约定: API类需要继承ApiBase这个基类
        ApiBase workapi = (ApiBase)Activator.CreateInstance(webapiT, true);
        workapi.SetHttpContext(context);

        // 到类中寻找方法
        MethodInfo webapiMethod = webapiT.GetMethods().FirstOrDefault(o => string.Compare(o.Name, apiMethod, true) == 0);

        // 未找到方法,结束请求!
        if (webapiMethod == null)
        {
            return ApiHandler.ErrorEnd(context, 5003, $"Method not found! [{apiClass}.{apiMethod}]");
        }

        // 检查方法是否贴有特性HTTPPOST/HTTPGET/HTTPALL. 例如[HTTPPOST],只响应POST请求.
        // 特性约定:做为API的方法需要贴上三个特性中的一个
        if (!AttributeCheck(context, webapiMethod))
        {
            return ApiHandler.ErrorEnd(context, 5004, $"Method not WebApi! [{apiClass}.{apiMethod}]");
        }

        // 方法返回值必须是Task
        if (webapiMethod.ReturnType != typeof(Task))
        {
            return ApiHandler.ErrorEnd(context, 5005, $"Method return type must Task! [{apiClass}.{apiMethod}]");
        }

        // 检查类或者方法上是否贴有AUTH特性,有则执行权限判断
        if (!AuthCheck(context, webapiMethod, webapiT, workapi))
        {
            return ApiHandler.ErrorEnd(context, 5006, $"Method Access Denied! [{apiClass}.{apiMethod}]");
        }

        // webapiMethod返回是Task类型,所以Invoke后得到一个Task,交给框架执行.
        // 不可以将webapiMethod再放到一个Task里,然后返回这个Task,否则就是在多个线程里了.
        // 会遇到的情况: 执行到方法里的第一个await时,框架就认为请求结束了,后面的await会执行,但是异常.
        //              涉嫌在请求结束后,使用HttpContent
        //              涉嫌多个线程操作httpContext,而它线程不安全...具体看文档
        // https://learn.microsoft.com/zh-cn/aspnet/core/performance/performance-best-practices?view=aspnetcore-7.0#do-not-access-httpcontext-from-multiple-threads
        // 另外,如果用同步的webapiMethod,则没这么多问题,但官方文档推荐异步读写请求...具体看文档
        // https://learn.microsoft.com/zh-cn/aspnet/core/performance/performance-best-practices?view=aspnetcore-7.0#avoid-synchronous-read-or-write-on-httprequesthttpresponse-body
        return webapiMethod.Invoke(workapi, null) as Task;
    }

    /// <summary>
    /// 自定义处理异常,返回简要异常信息.(中间件)
    /// 可以设置多个处理方法
    /// </summary>
    /// <returns></returns>
    internal static void CustomExceptMW(IApplicationBuilder exBuild)
    {
        static Task exHandler1(HttpContext context)
        {
            // 从上下文对象中获取异常对象.
            var exh = context.Features.Get<IExceptionHandlerPathFeature>();
            return ApiHandler.ErrorEnd(context, 5000, exh.Error.Message);
        }
        // Run是最后一个处理方法,如果还要传导,用Use()
        exBuild.Run(exHandler1);
    }

    /// <summary>
    /// 接口权限判断.当webapi类或其方法贴有AUTH特性时.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="webapiMethod"></param>
    /// <param name="webapiType"></param>
    /// <returns></returns>
    private static bool AuthCheck(HttpContext context, MethodInfo webapiMethod, Type webapiType, ApiBase webapiInstance)
    {
        if (Attribute.IsDefined(webapiType, typeof(AUTHAttribute)))
        {
            var auth = webapiType.GetCustomAttribute<AUTHAttribute>(false);
            return auth.Authenticate(context, webapiInstance);
        }
        if (Attribute.IsDefined(webapiMethod, typeof(AUTHAttribute)))
        {
            var auth = webapiMethod.GetCustomAttribute<AUTHAttribute>(false);
            return auth.Authenticate(context, webapiInstance);
        }
        return true;
    }

    /// <summary>
    /// 方法特性检查.是否贴有作为接口的三个特性
    /// 并非必要,只是为了加一个功能,让贴了特性的方法才能被访问.没贴的当内部方法
    /// </summary>
    /// <param name="webapiMethod">要检查的接口方法</param>
    /// <returns></returns>
    private static bool AttributeCheck(HttpContext context, MethodInfo webapiMethod)
    {
        string httpMethod = context.Request.Method.ToUpper();
        if (httpMethod == "POST" && Attribute.IsDefined(webapiMethod, typeof(HTTPPOSTAttribute)))
        {
            return true;
        }
        else if (httpMethod == "GET" && Attribute.IsDefined(webapiMethod, typeof(HTTPGETAttribute)))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// 工具方法: 返回错误信息,终结请求
    /// </summary>
    /// <param name="errcode"></param>
    /// <param name="errmsg"></param>
    /// <returns></returns>
    protected static Task ErrorEnd(HttpContext context, int errcode, string errmsg)
    {
        context.Response.ContentType = "application/json;charset=utf-8";
        // context.Response.StatusCode = 200;
        string errJsonStr = JsonConvert.SerializeObject(
            new
            {
                errmsg,
                errcode
            });
        return context.Response.WriteAsync(errJsonStr);
    }
}