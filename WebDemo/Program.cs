using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using WebDemo;
using cfg = WebDemo.Config;

// 加载配置选项
cfg.Load();

// webapp 设置
var webapp = (IApplicationBuilder app) =>
{
    // 生产环境异常处理
    //app.UseExceptionHandler(ApiHandler.CustomExceptMW);
    // 开发环境异常处理 (系统中间件)
    app.UseDeveloperExceptionPage();

    // 默认文档,静态文件 (系统中间件)
    app.UseDefaultFiles()
       .UseStaticFiles();

    // 提供wwwroot以外的其它虚拟目录(静态文件的) (系统中间件)
    if (cfg.VirtualDirsOptions != null)
    {
        foreach (var item in cfg.VirtualDirsOptions)
            app.UseStaticFiles(item);
    }

    // 跨域策略 (系统中间件)
    app.UseCors(cfg.CorsConfigBuild);

    // url映射到类的方法
    app.Run(ApiHandler.UrlMapMethodMW);
};

// 建立通用主机->载入配置->运行
var host = new HostBuilder();
host.ConfigureServices((IServiceCollection services) =>
{
    // 跨域服务
    services.AddCors();
    // MemoryCache内存缓存工具.在ApiBase.SetHttpContext里获取使用
    services.AddMemoryCache();

})
.ConfigureWebHost((IWebHostBuilder webHostBuild) =>
{
    // 设置web服务主机
    // 使用kestrel服务器
    webHostBuild.UseKestrel()
    // 配置监听端点
    .UseUrls(cfg.Urls)
    // 加入应用程序
    .Configure(webapp);
});
// 部署成windows服务
//host.UseWindowsService();
// 启动
host.Build()
    .Run();