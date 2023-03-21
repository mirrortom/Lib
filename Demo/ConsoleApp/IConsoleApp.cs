namespace Demo.ConsoleApp;

/// <summary>
/// 每个功能是一个命令,实现此接口
/// </summary>
public interface IConsoleApp
{
    /// <summary>
    /// 命令唯一编号.这个值就是命令,在控制台中输入.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 命令功能描述
    /// </summary>
    public string Desc { get; }

    /// <summary>
    /// 运行命令
    /// </summary>
    public void Run(params object[] args);
}