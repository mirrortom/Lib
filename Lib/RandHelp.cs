using System;
using System.Text;

namespace Lib;

/// <summary>
/// 用于产生随机的文本或者其它内容
/// </summary>
public class RandHelp
{
    /// <summary>
    /// 提供一个线程安全 Random 实例,该实例可同时从任何线程使用
    /// https://learn.microsoft.com/zh-cn/dotnet/api/system.random.shared?view=net-7.0#system-random-shared
    /// </summary>
    private static readonly Random rand = Random.Shared;

    /// <summary>
    /// 生成一个guid字符串
    /// </summary>
    /// <param name="style">1=大写,2=小写</param>
    /// <returns></returns>
    public static string NextGuid(byte style = 1)
    {
        string newguid = Guid.NewGuid().ToString("N");

        return style switch
        {
            2 => newguid.ToLower(),
            _ => newguid.ToUpper()
        };
    }

    /// <summary>
    /// 返回一个随机非负int整数,该数取下限而不取上限[start,end)
    /// </summary>
    /// <param name="start">大于0</param>
    /// <param name="end">大于0,大于start</param>
    /// <returns></returns>
    public static int NextInt(int start = 0, int end = 0)
    {
        // 参数不合法
        if (start > end || end <= 0 || start < 0)
            return rand.Next();
        return rand.Next(start, end);
    }

    /// <summary>
    /// 生成一个非负小数.该数取下限而不取上限[start,end).整数部分是long型
    /// </summary>
    /// <param name="dLen">小数位数1~32</param>
    /// <param name="start">大于0</param>
    /// <param name="end">大于0,大于start></param>
    /// <returns></returns>
    public static decimal NextDecimal(byte dLen = 1, long start = 0, long end = 0)
    {
        long intPart;
        // 参数不合法
        if (start > end || end <= 0 || start < 0)
            intPart = rand.NextInt64();
        else
            intPart = rand.NextInt64(start, end);
        //
        if (dLen < 1 || dLen > 32)
            dLen = 1;
        StringBuilder sb = new();
        for (int i = 0; i < dLen; i++)
        {
            sb.Append(rand.Next(0, 10));
        }
        // 组合整数和小数部分
        return decimal.Parse($"{intPart}.{sb}");
    }
    /// <summary>
    /// 生成一个随机的密码组成可选
    /// </summary>
    /// <param name="len">长度1~32</param>
    /// <param name="level">0=纯数字 1=纯小写字母 2=数字小写字母 3=数字大小写字母 4=数字字母大小写下划线加特殊</param>
    /// <returns></returns>
    public static string NextPwd(byte len, byte level)
    {
        if (len < 1) len = 1;
        if (level < 0 || level > 4) throw new ArgumentException("level = 0~5");
        string[] datasource = new string[27];
        datasource[0] = ConstVal.num10;
        datasource[1] = ConstVal.char26;
        datasource[2] = $"{ConstVal.num10}{ConstVal.char26}";
        datasource[3] = $"{ConstVal.num10}{ConstVal.char26}{ConstVal.CHAR26}";
        datasource[4] = $"{ConstVal.num10}{ConstVal.char26}{ConstVal.CHAR26}{ConstVal.schars}";
        //
        // 产生随机密码
        string newpwd()
        {
            StringBuilder sb = new();
            for (byte i = 0; i < len; i++)
            {
                sb.Append(datasource[level][rand.Next(0, datasource[level].Length)]);
            }
            return sb.ToString();
        }
        string pwd = newpwd();
        if (level < 2) return pwd;

        // 如果是组合型的,则需要判断是否含有对应组合中字符
        while (true)
        {
            // 判断随机数是否符合要求// 如果未随机出要求的字符,则需要重新随机.
            if (level == 2)
            {
                bool hasint = HasChar(pwd, ConstVal.num10);
                bool haschar = HasChar(pwd, ConstVal.char26);
                if (hasint && haschar)
                    return pwd;
            }
            else if (level == 3)
            {
                bool hasint = HasChar(pwd, ConstVal.num10);
                bool haschar = HasChar(pwd, ConstVal.char26);
                bool hasCHAR = HasChar(pwd, ConstVal.CHAR26);
                if (hasint && haschar && hasCHAR)
                    return pwd;
            }
            else if (level == 4)
            {
                bool hasint = HasChar(pwd, ConstVal.num10);
                bool haschar = HasChar(pwd, ConstVal.char26);
                bool hasCHAR = HasChar(pwd, ConstVal.CHAR26);
                bool hasschars = HasChar(pwd, ConstVal.schars);
                if (hasint && haschar && hasCHAR && hasschars)
                    return pwd;
            }
            pwd = newpwd();
        }
    }
    /// <summary>
    /// 检查指定字符串中是否包含另一字符串中的至少一个字符
    /// </summary>
    /// <param name="sourcestring">要检测的字符串</param>
    /// <param name="checkstring">是否包含这个字符</param>
    /// <returns></returns>
    private static bool HasChar(string sourcestring, string checkstring)
    {
        foreach (var item in checkstring)
        {
            if (sourcestring.Contains(item)) return true;
        }
        return false;
    }



    /// <summary>
    /// 返回一个随机的rgb字符串,16进制形式.如: "225588"
    /// </summary>
    /// <returns></returns>
    public static string NextHexColor()
    {
        byte[] c = new byte[3];
        rand.NextBytes(c);
        return Convert.ToHexString(c);
    }
}
