using System;
namespace WebDemo.Entity;
/// <summary>
/// 数据表: emp
/// </summary>
public class EmpM : BaseModel
{
    /// <summary>
    /// 登录账号(不可重复) maxlen=20
    /// </summary>
    public string Account;

    /// <summary>
    /// 身份唯一id号 maxlen=20
    /// </summary>
    public string Cardid;

    /// <summary>
    /// 人员名字 maxlen=20
    /// </summary>
    public string Uname;

    /// <summary>
    /// 昵称 maxlen=20
    /// </summary>
    public string Nickname;

    /// <summary>
    /// 性别 1=男 2=女
    /// </summary>
    public int Gender;

    /// <summary>
    ///  maxlen=4
    /// </summary>
    public string Birth;

    /// <summary>
    /// 头衔 maxlen=10
    /// </summary>
    public string Title;

    /// <summary>
    /// 11位手机(不可重复) maxlen=20
    /// </summary>
    public string Tel;

    /// <summary>
    /// 图片url,web相对目录,不带路径 maxlen=100
    /// </summary>
    public string Icon;

    /// <summary>
    /// 机构/组织guid maxlen=32
    /// </summary>
    public string Org;

    /// <summary>
    /// 角色id
    /// </summary>
    public int Role;

    /// <summary>
    /// 密码hash256salt maxlen=100
    /// </summary>
    public string Pwd;

    /// <summary>
    /// api权限列表 maxlen=500
    /// </summary>
    public string Power;

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime Utime;

}
