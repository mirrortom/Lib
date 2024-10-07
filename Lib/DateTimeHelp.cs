using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib;
public class DateTimeHelp
{
    /// <summary>
    /// 将时间转为unix时间戳:以格林威治时间1970年01月01日00时00分00秒开始,到该时间的秒数
    /// </summary>
    /// <param name="datetime"></param>
    /// <returns></returns>
    public static long DtToUnixTimeStamp(DateTime datetime = default)
    {
        // datetime可以隐式转为datetimeoffset
        DateTimeOffset dt = datetime == default ?
        DateTimeOffset.Now.LocalDateTime :
        datetime;
        return dt.ToUnixTimeSeconds();
    }

    /// <summary>
    /// 将unix时间戳(秒单位)转化为本地时区时间.
    /// </summary>
    /// <param name="unixTimeStamp"></param>
    /// <returns></returns>
    public static DateTime UnixTimeStampToLocalDt(long unixTimeStamp)
    {
        DateTimeOffset dto = DateTimeOffset.FromUnixTimeSeconds(unixTimeStamp);
        return dto.LocalDateTime;
    }
}
