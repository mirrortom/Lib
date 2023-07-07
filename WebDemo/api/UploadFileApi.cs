using System.Text;

namespace WebDemo;

// 上传文件,和分块上传文件示例
class UploadFileApi : ApiBase
{
    /// <summary>
    /// 文件保存路径服务器上的 据实际情况修改
    /// </summary>
    private readonly string FileDir = "E:/WWWROOT/uploadfile_dir";

    // 上传的文本保存成txt文件.文件按原文件名字保存
    [HTTPPOST]
    public async Task Index()
    {
        // 是否传了文本或文件
        StringBuilder log = new();

        Directory.CreateDirectory(this.FileDir);

        // 保存文本
        dynamic para = await this.ParaForm();
        if (((IDictionary<string, object>)para).ContainsKey("txtcont")
            && !string.IsNullOrWhiteSpace(para.txtcont))
        {
            string savePath = this.GetFileName(extName: "txt");
            using (StreamWriter sw = new(savePath))
            {
                sw.Write(para.txtcont);
            }
            //
            log.AppendLine($"文本已保存为txt文件[{savePath}]");
        }

        // 保存文件 用原来的文件名
        if (this.Request.Form.Files.Count > 0)
        {
            for (int i = 0; i < Request.Form.Files.Count; i++)
            {
                IFormFile file = this.Request.Form.Files[i];
                string filePath = this.GetFileName(file.FileName);
                using (var stream = System.IO.File.Create(filePath))
                {
                    await file.CopyToAsync(stream);
                }

                log.AppendLine($"已保存文件[{filePath}]");
            }
        }

        if (log.Length == 0)
        {
            await this.Json(new { errcode = 200, errmsg = "未上传任何内容!" });
            return;
        }
        await this.Json(new { errcode = 200, errmsg = log.ToString() });
        //
    }

    // 保存大文件,分块接收,最后拼接成一个文件
    [HTTPPOST]
    public async Task BigFile()
    {
        // 保存文件 用原来的文件名
        if (this.Request.Form.Files.Count > 0)
        {
            // file.Name上传时约定名字 partIndex|Guid , -1|Guid|name(上传完成时)
            // index是部分块编号,前端按顺序读取生成,0表示首次上传. guid是同一个文件标志,首次上传时,由服务端生成传给客户端
            IFormFile file = this.Request.Form.Files[0];
            string[] fnames = file.Name.Split('|');

            // 检查部分块文件名
            if (!int.TryParse(fnames[0], out int fPartIndex))
            {
                await this.Json(new { errcode = 501, errmsg = "部分块编号无效!" });
                return;
            }
            if (!Guid.TryParse(fnames[1], out Guid tmpGuid))
            {
                if (fPartIndex > 0)
                {
                    await this.Json(new { errcode = 502, errmsg = "部分块guid无效!" });
                    return;
                }
            }

            // -1|Guid|name(上传结束标志)
            if (fPartIndex == -1)
            {
                if (fnames.Length != 3 || string.IsNullOrWhiteSpace(fnames[2]))
                {
                    await this.Json(new { errcode = 503, errmsg = "上传结束标志缺少文件名!" });
                    return;
                }
                string mergeFile = this.CombineFile(fnames[1], fnames[2]);
                if (string.IsNullOrWhiteSpace(mergeFile))
                {
                    await this.Json(new { errcode = 504, errmsg = $"文件合并失败!({mergeFile})" });
                    return;
                }
                // 合并文件,返回结果
                await this.Json(new { errcode = 200 });
                return;
            }

            // 保存第一个部分块 编号==0时,表示文件第一块上传
            if (fPartIndex == 0)
            {
                // 建立一个临时文件夹
                string newguid = Guid.NewGuid().ToString();
                Directory.CreateDirectory($"{this.FileDir}/{newguid}");
                // 临时文件以部分块编号为文件名
                string fPath = this.GetPartFileName(newguid, fPartIndex);
                using (var newStream = System.IO.File.Create(fPath))
                {
                    file.CopyTo(newStream);
                }
                await this.Json(new { errcode = 200, guid = newguid, errmsg = $"文件首次上传成功!" });
                return;
            }

            // 保存其它部分块
            string filePath = this.GetPartFileName(fnames[1], fPartIndex);
            using (var stream = System.IO.File.Create(filePath))
            {
                file.CopyTo(stream);
            }
            await this.Json(new { errcode = 200, errmsg = $"({fPartIndex}) 号部分块上传成功" });
            return;
        }

        //
        await this.Json(new { errcode = 200, errmsg = "没有文件上传" });
    }

    /// <summary>
    /// 返回文件全路径名字.
    /// 如果没有文件名字,将生成随机文件名字
    /// 文件名字前缀是当前日期时间
    /// </summary>
    /// <returns></returns>
    private string GetFileName(string name = "", string extName = "")
    {
        string preN = DateTime.Now.ToString("yyyy-MM-dd_HHmm");
        string n = name == "" ? Path.GetRandomFileName() : name;
        string extN = extName;
        // 判断扩展名参数是否带.号
        if (extN != "" && !extN.StartsWith('.'))
        {
            extN = $".{extN}";
        }
        // 时间+随机名字
        return $"{this.FileDir}/{preN}-{n}.{extN}";
    }

    /// <summary>
    /// 返回部分块文件名,位于临时文件夹的
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    private string GetPartFileName(string tmpDirGuid, int partIndex)
    {
        return $"{this.FileDir}/{tmpDirGuid}/{partIndex}";
    }


    /// <summary>
    /// 将临时文件夹里的所有部分块结合成整个文件 返回文件名
    /// </summary>
    /// <param name="tmpDirGuid"></param>
    /// <returns></returns>
    private string CombineFile(string tmpDirGuid, string fName)
    {
        string tmpDir = $"{this.FileDir}/{tmpDirGuid}";
        if (Directory.Exists(tmpDir))
        {
            string[] files = Directory.GetFiles(tmpDir);
            string filePath = this.GetFileName(fName);
            var count = files.Length;
            // 从名为0的文件块开始
            using (var stream = new FileStream(filePath, FileMode.OpenOrCreate))
            {
                for (int i = 0; i < count; i++)
                {
                    string file = $"{tmpDir}/{i}";
                    if (System.IO.File.Exists(file))
                    {
                        stream.Write(System.IO.File.ReadAllBytes(file));
                    }
                }
            }
            return filePath;
        }
        return "";
    }
}
