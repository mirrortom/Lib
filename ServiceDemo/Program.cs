//====================================================================
// 用于定时执行任务,可以安装为Windows服务.
// 在linux上,可以用systemd管理工具安装为服务启动方式
//====================================================================
using Lib;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServiceDemo;
NLogHelp.Init();

IHost host = new HostBuilder()
    // 在windows上,如果需要安装为windows服务,则开启
    //.UseWindowsService()
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
    })
    .Build();
await host.RunAsync();

// 其它问题记录
// 1.发布为win-64,选择"独立",最后部署到win服务,结果程序执行有问题,而在控制台调试或者直接执行exe都没有问题,打印异常后,
// 发现错误居然是"空引用没有实例",感觉应该是"独立"发布后的文件出了问题,可能是少了一些库文件.最后选"依赖"发布,正常了.