using Microsoft.AspNetCore.Diagnostics;
using Newtonsoft.Json;
using System.Reflection;

namespace WebDemo;

// url解析中间件,异常处理中间件
internal class ApiHandler
{
    /// <summary>
    /// 分析url地址,查找然后执行对应的类和方法.(中间件)
    /// </summary>
    /// <returns></returns>
    internal static Task UrlMapMethodMW(HttpContext context)
    {
        // 分解URL路径用于找类名和方法名
        var url = ParseUrl(context);
        if (url.apiCls == null)
        {
            return ApiHandler.ErrorEnd(context, 5001, $"Url invalid! [{context.Request.Path.Value}]");
        }

        // 到当前程序集中查找匹配的类
        var apiType = QueryApiClass(url.apiCls);
        if (apiType == null)
        {
            return ApiHandler.ErrorEnd(context, 5002, $"No class found,no match or not extend ApiBase. [{url.apiCls}]");
        }

        // 到类中查找匹配的方法.
        var methods = QueryApiMethod(apiType, url.apiMethod);
        if (methods == null)
        {
            return ApiHandler.ErrorEnd(context, 5003, $"No match Method found! [{apiType.FullName}.{url.apiMethod}]");
        }

        // 检查 方法是否有特性
        var mInfoArr = ApiMethodsFeatureCheck(methods, context.Request.Method.ToUpper());
        if (mInfoArr.Length == 0)
        {
            return ApiHandler.ErrorEnd(context, 5004, $"No method found,feature not match![{apiType.FullName}.{url.apiMethod}]");
        }

        // 检查 方法返回类型是否为Task
        mInfoArr = ApiMethodsReTypeCheck(mInfoArr);
        if (mInfoArr.Length == 0)
        {
            return ApiHandler.ErrorEnd(context, 5005, $"No method found,return type not match![{apiType.FullName}.{url.apiMethod}]");
        }

        // 确定要执行的那一个方法,如果有重载时
        var apiMInfo = ApiMethodOverloadCheck(mInfoArr);
        if (apiMInfo.method == null)
        {
            return ApiHandler.ErrorEnd(context, 5006, $"No method found,overload not match![{apiType.FullName}.{url.apiMethod}]");
        }

        // 进行权限检查(对于贴有[AUTH]的类和方法)
        if (!ApiAuthCheck(context, apiMInfo.method, apiType))
        {
            return ApiHandler.ErrorEnd(context, 5007, $"Access Denied,method or class![{apiType.FullName}.{url.apiMethod}]");
        }

        // 实例化类,设定数据
        ApiBase workapi = (ApiBase)Activator.CreateInstance(apiType, true);
        workapi.SetHttpContext(context);

        // UrlMapMethodMW需要返回是Task类型,所以Invoke方法包装为一个Task返回.框架会执行.
        // 不可以将webapiMethod再放到一个Task里,然后返回这个Task,否则就是在多个线程里了.
        // 会遇到的情况: 执行到方法里的第一个await时,框架就认为请求结束了,后面的await会执行,但是异常.
        //              涉嫌在请求结束后,使用HttpContent
        //              涉嫌多个线程操作httpContext,而它线程不安全...具体看文档
        // https://learn.microsoft.com/zh-cn/aspnet/core/performance/performance-best-practices?view=aspnetcore-7.0#do-not-access-httpcontext-from-multiple-threads
        // 另外,如果用同步的webapiMethod,则没这么多问题,但官方文档推荐异步读写请求...具体看文档
        // https://learn.microsoft.com/zh-cn/aspnet/core/performance/performance-best-practices?view=aspnetcore-7.0#avoid-synchronous-read-or-write-on-httprequesthttpresponse-body

        // 执行方法
        return apiMInfo.method.Invoke(workapi, apiMInfo.paras) as Task;
    }

    /// <summary>
    /// 检查方法重载,选取匹配的一个重载,并且生成参数数组,在调用时使用.
    /// </summary>
    private static (MethodInfo method, object[] paras) ApiMethodOverloadCheck(MethodInfo[] minfoArr)
    {
        // 目前不支持重载玩法,匹配重载比较麻烦.
        //     目前实现了POST/GET两种Http请求方式,所以至多需要写2个重载,每个方法贴一种特性,
        // 确保(方法名字+请求特性)的组合是唯一的.
        //     这是为了可以实现同名字方法,一个处理GET请求,一个处理POST请求.
        //     如果支持Create,Delete这些了,那么有几种请求方式,至多需要写几个重载

        MethodInfo m = null;
        // 没有重载情况:这是正常情况
        if (minfoArr.Length == 1)
            m = minfoArr[0];

        // 不幸写了多个重载时,返回无参数的重载方法
        if (m == null)
        {
            m = minfoArr.FirstOrDefault(m => m.GetParameters().Length == 0);
        }

        // 没有无参重载,不再匹配,直接报错
        if (m == null)
            return (null, null);

        // 参数处理:有默认值使用默认值.没有时,以参数类型默认值填充.
        object[] paras = null;
        if (m.GetParameters().Length > 0)
        {
            paras = m.GetParameters().Select(o =>
            {
                if (o.HasDefaultValue)
                    return o.DefaultValue;
                // 建立值类型的实例,可以得到值类型的默认值
                return o.ParameterType.IsValueType ?
                       Activator.CreateInstance(o.ParameterType) : null;
            }).ToArray();
        }

        return (m, paras);
    }

    /// <summary>
    /// 检查方法返回类型
    /// </summary>
    /// <param name="minfoArr"></param>
    /// <returns></returns>
    private static MethodInfo[] ApiMethodsReTypeCheck(MethodInfo[] minfoArr)
    {
        return minfoArr.Where(m => m.ReturnType == typeof(Task)).ToArray();
    }

    /// <summary>
    /// 检查方法特性
    /// </summary>
    /// <returns></returns>
    private static MethodInfo[] ApiMethodsFeatureCheck(MethodInfo[] methods, string httpMethod)
    {
        // 贴有的[HTTPGET]或[HTTPPOST]与请求方法类型匹配,例如GET请求匹配[HTTPGET]
        Dictionary<string, Func<MethodInfo, bool>> checkDict = new();
        checkDict.Add("POST", (minfo) =>
        {
            return Attribute.IsDefined(minfo, typeof(HTTPPOSTAttribute));
        });
        checkDict.Add("GET", (minfo) =>
        {
            return Attribute.IsDefined(minfo, typeof(HTTPGETAttribute));
        });

        return methods.Where(
                m => checkDict.TryGetValue(httpMethod, out var check) && check(m)).
            ToArray();
    }

    /// <summary>
    /// 查找匹配的方法,在找到的类中
    /// </summary>
    /// <param name="apiType"></param>
    /// <returns></returns>
    private static MethodInfo[] QueryApiMethod(Type apiType, string methodName)
    {
        // 再到类中寻找方法,有重载时,会找到多个方法
        MethodInfo[] methods = apiType.GetMethods().Where
        (minfo => string.Compare(minfo.Name, methodName, true) == 0).ToArray();
        // 未找到方法
        if (methods.Length == 0)
        {
            return null;
        }
        return methods;
    }

    /// <summary>
    /// 查找匹配的类,在当前程序集中.必须继承ApiBase类
    /// </summary>
    /// <param name="apiCls"></param>
    /// <returns></returns>
    private static Type QueryApiClass(string apiClsName)
    {
        // 程序集名字,也是默认命名空间名字.
        string defNs = Assembly.GetExecutingAssembly().GetName().Name;
        // 类一般在默认命名空间下,在前面添加默认命名空间后查找.
        // 没有,再去掉默认命名空间查找
        Type apiType =
            Assembly.GetExecutingAssembly().GetType($"{defNs}.{apiClsName}", false, true)
         ?? Assembly.GetExecutingAssembly().GetType($"{apiClsName}", false, true);

        // 还没有,报错
        if (apiType == null)
        {
            return null;
        }

        // 必须继承ApiBase类
        return apiType.BaseType == typeof(ApiBase) ? apiType : null;
    }

    /// <summary>
    /// 分析url地址,返回用于匹配的类名和方法名
    /// </summary>
    /// <returns></returns>
    private static (string apiCls, string apiMethod) ParseUrl(HttpContext context)
    {
        // Path.Value值是路径,不含协议名字(http),主机名字(www.xx.com),?后面的参数(?a=1)
        // 例:"/api/emp/getlist"
        string[] urlparts = context.Request.Path.Value.Split('/');

        // 匹配规则: 倒数1段是方法名,例:getlist, 倒数2段是类名, 前面部分是类的命名空间
        // 如果开启了静态文件,比如/api/emp/index.html,但文件没找到时,也会进入这里处理
        if (urlparts[^1].Contains('.') || urlparts.Length < 3)
        {
            return (null, null);
        }

        // api类名全称是- [命名空间].类名Api(约定后缀).例:[MyWebApi.api].empApi
        // 后缀约定:一个成为api的类,结尾为Api.例如: UserApi
        string apiClass = string.Join('.', urlparts[1..^1]) + "Api";
        string apiMethod = urlparts[^1];
        return (apiClass, apiMethod);
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
    private static bool ApiAuthCheck(HttpContext context, MethodInfo webapiMethod, Type webapiType)
    {
        // 如果类上贴了[AUTH],类里面所有方法都放行或者拒绝,无需再一个个贴.
        if (Attribute.IsDefined(webapiType, typeof(AUTHBaseAttribute)))
        {
            var auth = webapiType.GetCustomAttribute<AUTHBaseAttribute>(false);
            return auth.Authenticate(context);
        }
        // 只在方法上贴特性,验证只对单个方法
        if (Attribute.IsDefined(webapiMethod, typeof(AUTHBaseAttribute)))
        {
            var auth = webapiMethod.GetCustomAttribute<AUTHBaseAttribute>(false);
            return auth.Authenticate(context);
        }
        return true;
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