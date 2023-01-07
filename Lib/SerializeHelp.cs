using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Lib;

/// <summary>
/// JSON的转换全部换成了newtonsoft.json的方法
/// </summary>
public class SerializeHelp
{
    /// <summary>
    /// 用指定字段(或者属性),建立新对象列表.返回对象列表的json字符串
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="objlist"></param>
    /// <param name="fieldNames"></param>
    /// <returns></returns>
    public static string ObjectsToJSON<T>(IEnumerable<T> objlist, params string[] fieldNames)
    {
        // 没有指定字段,返回所有字段
        if (fieldNames.Length == 0)
            return JsonConvert.SerializeObject(objlist);
        //
        var dictlist = ObjectsToDicts(objlist, fieldNames);
        return JsonConvert.SerializeObject(dictlist);
    }
    /// <summary>
    /// 用指定字段(或者属性),建立新对象.返回对象的json字符串
    /// </summary>
    /// <typeparam name="T">源对象类型</typeparam>
    /// <param name="obj">源对象</param>
    /// <param name="fieldName">源对象字段(属性)</param>
    /// <returns></returns>
    public static string ObjectToJSON<T>(T obj, params string[] fieldNames)
    {
        // 没有指定字段,返回所有字段
        if (fieldNames.Length == 0)
            return JsonConvert.SerializeObject(obj);
        //
        var dict = ObjectToDict(obj, fieldNames);
        return JsonConvert.SerializeObject(dict);
    }
    /// <summary>
    /// 将对象列表转化为字典列表.可以指定部分字段(属性)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="objlist"></param>
    /// <param name="fieldNames"></param>
    /// <returns></returns>
    public static List<Dictionary<string, object>> ObjectsToDicts<T>(IEnumerable<T> objlist, params string[] fieldNames)
    {
        List<Dictionary<string, object>> dictlist = new();
        foreach (T item in objlist)
        {
            dictlist.Add(ObjectToDict(item, fieldNames));
        }
        return dictlist;
    }
    /// <summary>
    /// 将对象转化为字典.可以指定部分字段(属性)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <param name="fieldNames"></param>
    /// <returns></returns>
    public static Dictionary<string, object> ObjectToDict<T>(T obj, params string[] fieldNames)
    {
        List<string> pnames = new List<string>();
        if (fieldNames.Length > 0)
            pnames.AddRange(fieldNames);
        else
        {
            System.Reflection.FieldInfo[] fields = obj.GetType().GetFields();
            System.Reflection.PropertyInfo[] props = obj.GetType().GetProperties();
            pnames.AddRange(fields.Select(o => o.Name));
            pnames.AddRange(props.Select(o => o.Name));
        }
        Dictionary<string, object> dict = new Dictionary<string, object>();
        for (int i = 0, len = pnames.Count; i < len; i++)
        {
            string pname = pnames[i];
            System.Reflection.FieldInfo field = obj.GetType().GetField(pname,
                                  System.Reflection.BindingFlags.Public |
                                  System.Reflection.BindingFlags.Instance |
                                  System.Reflection.BindingFlags.IgnoreCase);
            if (field != null)
            {
                dict.Add(field.Name, field.GetValue(obj));
                continue;
            }
            // 使用反射找到出该字段名称相同的对象的属性
            System.Reflection.PropertyInfo prop = obj.GetType().GetProperty(pname,
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.IgnoreCase);
            if (prop != null)
            {
                dict.Add(prop.Name, prop.GetValue(obj));
            }
        }
        return dict;
    }
    /// <summary>
    /// 转成动态类型对象可以像JS对象一样,增减对象成员.
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public static dynamic JSONToDynamic(string json)
    {
        return JsonConvert.DeserializeObject(json);
    }
    /// <summary>
    /// 转化为指定类型对象
    /// 如果JSON数值不能转为T的对应类型值,则会异常
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="json"></param>
    /// <returns></returns>
    public static T JsonToObject<T>(string json)
    {
        try
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
        catch (Exception)
        {
            return default;
        }
    }

    /// <summary>
    /// 将一个XMLELEMENT节点转换为指定对象
    /// <para>注意:XML节点的属性名字必须和类属性名一样(大小写不限).节点规范,不含注释等杂项</para>
    /// <para>本方法并不检测数据有效性,须要保证传入的节点元素对象有效!</para>
    /// 节点示例:(Student id="0" name="tom" country="中国")(/Student)
    /// </summary>
    /// <typeparam name="T">将要转成的对象类型</typeparam>
    /// <param name="xmlnode">需要转换的节点对象</param>
    /// <returns></returns>
    public static T XmlNodeToObject<T>(XmlNode xmlnode)
    {
        // 创建一个实例
        T tmp = System.Activator.CreateInstance<T>();
        // 循环当行数据行的所有字段
        for (int i = 0; i < xmlnode.Attributes.Count; i++)
        {
            XmlAttribute currattr = xmlnode.Attributes[i];
            // 使用反射找到出该字段名称相同的对象的属性
            System.Reflection.PropertyInfo prop = tmp.GetType().GetProperty(currattr.Name,
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.IgnoreCase);
            // 如果找到了属性,则设置值.没找到则不动作(此处可能转换类型失败,请尽量保证属性类型为string)
            if (prop != null)
            {
                prop.SetValue(tmp, Convert.ChangeType(currattr.Value, prop.PropertyType));
            }
        }
        return tmp;
    }
}
