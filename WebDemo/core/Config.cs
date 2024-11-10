using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.FileProviders;

namespace WebDemo;

/// <summary>
/// 载入配置,并提供配置项
/// </summary>
internal class Config
{
    /// <summary>
    /// 开启静态文件,true=开启
    /// </summary>
    internal static bool EnableStatic { get; private set; }
    // 监听地址
    internal static string[] Urls { get; private set; }
    // 默认文档配置项
    internal static DefaultFilesOptions DefaultDocOptions { get; private set; }
    // 虚拟目录
    internal static List<StaticFileOptions> VirtualDirsOptions { get; private set; }

    /// <summary>
    /// 加载配置
    /// </summary>
    internal static void Load()
    {
        // 读取自定义配置json
        IConfiguration set = new ConfigurationBuilder()
            .AddJsonFile("settings.json")
            .Build();
        var cfg = new Config();

        // 是否开启静态文件
        Config.EnableStatic = set.GetValue("enableStatic", 0) == 1;

        // 默认文档配置项
        string[] docs = set.GetSection("defaultDoc").Get<string[]>();
        Config.DefaultDocOptionsSet(docs);

        // urls监听地址
        Config.Urls = set.GetSection("urls").Get<string[]>();

        // 虚拟目录
        var virtualDirs = set.GetSection("virtualDir").Get<Dictionary<string, string>[]>();
        Config.VirtualDirsOptionsSet(virtualDirs);
    }

    /// <summary>
    /// 默认文档设定
    /// </summary>
    /// <param name="docNames"></param>
    private static void DefaultDocOptionsSet(string[] docNames)
    {
        if (docNames == null || docNames.Length == 0) return;
        Config.DefaultDocOptions = new();
        // DefaultFileNames有默认值,这里重新new,去掉默认值.
        DefaultDocOptions.DefaultFileNames = [];
        foreach (var item in docNames)
        {
            DefaultDocOptions.DefaultFileNames.Add(item);
        }
    }

    // 静态文件配置项
    // 文档: https://learn.microsoft.com/zh-cn/aspnet/core/fundamentals/static-files?view=aspnetcore-7.0
    /// <summary>
    /// 虚拟目录设定
    /// </summary>
    /// <param name="virtualDir"></param>
    private static void VirtualDirsOptionsSet(Dictionary<string, string>[] virtualDir)
    {
        // virtualDir: [{fdir:"物理路径(相对web根目录)",refdir:"url映射路径,斜杠/打头"}]
        if (virtualDir == null || virtualDir.Length == 0)
            return;
        VirtualDirsOptions = [];
        foreach (var item in virtualDir)
        {
            VirtualDirsOptions.Add(new StaticFileOptions()
            {
                // 这里配置物理目录
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(AppContext.BaseDirectory, item["fdir"])),
                // 配置对应虚拟目录,就是url请求上的目录
                RequestPath = item["refdir"]
            });
        }
    }

    /// <summary>
    /// 跨域策略 选项
    /// https://learn.microsoft.com/zh-cn/aspnet/core/security/cors?view=aspnetcore-6.0#uc1
    /// </summary>
    /// <param name="option"></param>
    internal static void CorsConfigBuild(CorsPolicyBuilder option)
    {
        option.AllowAnyHeader();
        option.AllowAnyMethod();
        option.AllowAnyOrigin();
        //cfg.AllowCredentials();
    }
}
