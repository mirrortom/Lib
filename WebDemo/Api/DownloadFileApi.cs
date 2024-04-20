using System.Text;

namespace WebDemo;

// 上传文件,和分块上传文件示例
class DownloadFileApi : ApiBase
{
    /// <summary>
    /// 要下载的文件的存放路径在服务器上的 据实际情况修改
    /// </summary>
    private readonly string FileDir = "E:/WWWROOT/tophone_dir";

    [HTTPPOST]
    public async Task Index()
    {
        // 获取目录的文件列表,提供下载,
        // 只取第一级目录,目录和子目录忽略
        string[] files = Directory.GetFiles(FileDir);
        List<string> result = new();
        foreach (string file in files)
        {
            result.Add(Path.GetFileName(file));
        }
        await this.Json(new { result });
    }

    /// <summary>
    /// 下载文件
    /// </summary>
    /// <returns></returns>
    [HTTPPOST]
    [HTTPGET]
    public async Task Down()
    {
        var para = this.ParaDictGET();
        if (para.ContainsKey("id"))
        {
            string name = para["id"]?.ToString();
            if (!string.IsNullOrWhiteSpace(name))
            {
                string file = Path.Combine(FileDir, name);
                if (System.IO.File.Exists(file))
                {
                    await this.File(file, "application/octet-stream",name);
                    return;
                }
            }
        }
        await this.Json(new { errcode = 501 });
    }
}
