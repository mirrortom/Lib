namespace Demo.ConsoleApp.cmds;

public class TestCmd : IConsoleApp
{
    public string Name => "test";
    public string Desc => "这是一个测试方法";

    public void Run(params object[] args)
    {
        Console.WriteLine("增加命令类,需要实现ICommand接口.");
    }
}