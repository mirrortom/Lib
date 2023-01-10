namespace WebDemo.services;

public class TestInfoService:IHostedService
{
    private readonly IWebHostEnvironment WebHostEnv;
    private readonly IConfiguration Configuration;
    public TestInfoService(IWebHostEnvironment webHostEnv, IConfiguration configuration)
    {
        WebHostEnv = webHostEnv;
        Configuration = configuration;
    }
    public Dictionary<string, string> GetInfo()
    {
        string urls = Configuration.GetValue<string>(WebHostDefaults.ServerUrlsKey);
        return new Dictionary<string, string> {
            { "名字 AppName", WebHostEnv.ApplicationName },
            { "程序运行目录 AppContext.BaseDirectory", AppContext.BaseDirectory },
            {"内容根目录 ContentRootPath",WebHostEnv.ContentRootPath },
            {"web服务器 server","Kestrel" },
            {"web根目录 WebRootPath",WebHostEnv.WebRootPath },
            {"web服务器监听 server listen",urls },
        };
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        //
        string n = Environment.NewLine;
        Console.WriteLine($"--应用信息--");
        var dict = GetInfo();
        foreach (var k in dict.Keys)
        {
            Console.WriteLine($"#{k} {n}{dict[k]}{n}");
        }
        Console.WriteLine($"#{n}打开浏览器进行测试. ctrl + c 退出.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
