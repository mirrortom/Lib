using Lib;

namespace Demo;

/// <summary>
/// 配置模板 静态类,load方法加载配置文件,使用CfgHelp工具类读取配置文件,然后做设置到属性或者其它工作
/// </summary>
public static class Setting
{
    public static void Load(string file)
    {
        string extN = Path.GetExtension(file);
        if (extN == ".json")
        {
            // json
            dynamic json = CfgHelp.ReadJson(file);
            foreach (var property in json)
            {
                Console.Write(property.Name);
                Console.Write('\t');
                Console.WriteLine(property.Value);
            }
        }
        else if (extN == ".ini")
        {
            // ini
            var ini = CfgHelp.Readini(file);
            foreach (var k in ini.Keys)
            {
                Console.Write(k);
                Console.Write('\t');
                Console.WriteLine(ini[k]);
            }
        }

    }
}
