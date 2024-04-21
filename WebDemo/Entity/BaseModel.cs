namespace WebDemo.Entity;

public class BaseModel
{
    #region  属性
    /// <summary>
    /// 表名 对应的数据表名字
    /// </summary>
    public virtual string Table { get; set; }
    /// <summary>
    /// 表数据状态值
    /// </summary>
    public virtual int Status { get; set; }
    /// <summary>
    /// 表数据主键
    /// </summary>
    public virtual string Id { get; set; }
    /// <summary>
    /// 字段(属性)的名字列表,逗号隔开的字符串,用于在webapi返回json对象时,指定要返回的字段(属性)
    /// </summary>
    public virtual string Fields { get; set; }
    /// <summary>
    /// 参数字典,用于sql查询参数
    /// </summary>
    public virtual Dictionary<string, object> SqlPara { get; set; }
    /// <summary>
    /// 是否需要分页,0=不需要分页,其它值=分页
    /// </summary>
    public virtual int IsPagePart { get; set; }
    /// <summary>
    /// 表示行号
    /// </summary>
    public virtual int RowNumber { get; set; }
    /// <summary>
    /// 表示当前页码
    /// </summary>
    public virtual int PageIndex { get; set; }
    /// <summary>
    /// 表示每一分页显示的记录数
    /// </summary>
    public virtual int PageSize { get; set; }
    /// <summary>
    /// 表示记录总数
    /// </summary>
    public virtual int ListCount { get; set; }
    /// <summary>
    /// 表示在指定页码时,数据记录的起始行号 2016年8月27日 17:45:04
    /// </summary>
    /// <returns></returns>
    public virtual int StartRowIndex
    {
        get { return (this.PageIndex - 1) * this.PageSize + 1; }
    }
    /// <summary>
    /// 表示在指定页码和分页大小时,数据记录的结束行号 
    /// </summary>
    /// <returns></returns>
    public virtual int EndRowIndex
    {
        get { return this.PageIndex * this.PageSize; }
    }
    /// <summary>
    /// 表示在使用offset办法分页时,应该忽略的行数
    /// </summary>
    public virtual int OffSetRows
    {
        get { return (this.PageIndex - 1) * this.PageSize; }
    }
    /// <summary>
    /// 总页数(由数量总数和分页大小算出)
    /// </summary>
    public virtual int PageCount
    {
        get
        {
            if (this.ListCount >= 0 && this.PageSize >= 1 && this.PageIndex >= 1)
            {
                int pagecount = this.ListCount / this.PageSize;
                int pagecountM = this.ListCount % this.PageSize;
                int pagesum = pagecountM > 0 ? pagecount + 1 : pagecount;
                return pagesum;
            }
            return 0;
        }
    }
    /// <summary>
    /// 表示前一页码(应由当前页码计算得出)
    /// </summary>
    public virtual int PrevPage
    {
        get { return this.PageIndex <= 1 ? 1 : this.PageIndex - 1; }
    }
    /// <summary>
    /// 表示后一页码
    /// </summary>
    public virtual int NextPage
    {
        get { return this.IsEndPage ? this.PageIndex : this.PageIndex + 1; }
    }
    /// <summary>
    /// 表示当前是否为第一页(由pageindex算出)
    /// </summary>
    public virtual bool IsStartPage
    {
        get { return this.PageIndex <= 1; }
    }
    /// <summary>
    /// 表示是否已经到最后一页(由pagesize,pageindex和总数量算出,如果未设定这三值,则返回false)
    /// </summary>
    public virtual bool IsEndPage
    {
        get
        {
            if (this.ListCount >= 0 && this.PageSize >= 1 && this.PageIndex >= 1)
            {
                return this.PageIndex * this.PageSize >= this.ListCount;
            }
            return false;
        }
    }
    /// <summary>
    /// 错误提示信息
    /// </summary>
    public virtual string ErrorMsg { get; set; }
    /// <summary>
    /// 错误提示代码 约定200=成功 
    /// </summary>
    public virtual int ErrorCode { get; set; }
    #endregion

    #region 方法
    /// <summary>
    /// 检查和设定当前页码数和分页大小 
    /// 1.传参则检查参数有效性,否则检查pageindex,pagesize属性值有效性
    /// 2.若值有效则更新属性值,无效则设定默认属性值(pageindex=1,pagesize=10)
    /// 调用此方法后,页码和分页大小将为有效值
    /// </summary>
    public virtual void SetPageIndexAndSize(string pindex = null, string psize = null)
    {
        if (string.IsNullOrWhiteSpace(pindex))
        {
            if (this.PageIndex <= 0)
                this.PageIndex = 1;
        }
        else
        {
            int pn;
            this.PageIndex = int.TryParse(pindex, out pn) && pn > 0 ? pn : 1;
        }
        //
        if (string.IsNullOrWhiteSpace(psize))
        {
            if (this.PageSize <= 0)
                this.PageSize = 10;
        }
        else
        {
            int pz;
            this.PageSize = int.TryParse(psize, out pz) && pz > 0 ? pz : 10;
        }
    }
    #endregion
}
